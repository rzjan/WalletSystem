using WalletSystem.Worker.Messaging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace WalletSystem.Worker.Consumer;

public sealed class EventConsumerService : BackgroundService
{
    private const string RetryCountHeader = "x-retry-count";

    private readonly RabbitMqOptions _mq;
    private readonly ConsumerOptions _opts;
    private readonly InboxProcessor _processor;
    private readonly ILogger<EventConsumerService> _logger;

    private IConnection? _connection;
    private IModel? _channel;

    public EventConsumerService(
        IOptions<RabbitMqOptions> mq,
        IOptions<ConsumerOptions> opts,
        InboxProcessor processor,
        ILogger<EventConsumerService> logger)
    {
        _mq = mq.Value;
        _opts = opts.Value;
        _processor = processor;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await ConnectAsync(stoppingToken);

        try { await Task.Delay(Timeout.Infinite, stoppingToken); }
        catch (OperationCanceledException) { /* apagado normal */ }
    }

    private async Task ConnectAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                _connection = _mq.ToConnectionFactory(asyncConsumers: true)
                    .CreateConnection("wallet-event-consumer");
                _channel = _connection.CreateModel();
                _channel.ConfirmSelect();   // para confirmar las publicaciones a retry / DLQ

                RabbitTopology.Declare(_channel, _mq, _opts);
                _channel.BasicQos(0, _opts.Prefetch, global: false);

                var consumer = new AsyncEventingBasicConsumer(_channel);
                consumer.Received += (_, ea) => OnReceivedAsync(ea, ct);
                _channel.BasicConsume(_opts.QueueName, autoAck: false, consumer);

                _logger.LogInformation(
                    "Consumer escuchando {Queue} (prefetch {Prefetch}, {Retries} reintentos cada {Delay} ms)",
                    _opts.QueueName, _opts.Prefetch, _opts.MaxRetries, _opts.RetryDelayMs);
                return;
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "No se pudo iniciar el consumer; se reintenta en 5 s");
                _channel?.Dispose();
                _connection?.Dispose();
                await Task.Delay(TimeSpan.FromSeconds(5), ct);
            }
        }
    }

    private async Task OnReceivedAsync(BasicDeliverEventArgs ea, CancellationToken ct)
    {
        var retryCount = GetRetryCount(ea.BasicProperties);
        var type = ea.BasicProperties.Type ?? "desconocido";

        try
        {
            if (!Guid.TryParse(ea.BasicProperties.MessageId, out var messageId))
                throw new PermanentFailureException("El mensaje no trae un MessageId válido");

            var payload = Encoding.UTF8.GetString(ea.Body.Span);
            var context = new EventContext(messageId, type, payload, Attempt: retryCount + 1);

            var result = await _processor.ProcessAsync(context, ct);

            if (result == ProcessResult.Duplicate)
                _logger.LogWarning("Duplicado ignorado: {MessageId}", messageId);

            _channel!.BasicAck(ea.DeliveryTag, multiple: false);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            // Si estamos apagando, no se hace nada: sin ack, el broker reentrega el mensaje.
            HandleFailure(ea, ex, retryCount, type);
        }
    }

    private void HandleFailure(BasicDeliverEventArgs ea, Exception ex, int retryCount, string type)
    {
        var permanent = ex is PermanentFailureException;
        var toDeadLetter = permanent || retryCount >= _opts.MaxRetries;
        var error = ex.Message.Length > 500 ? ex.Message[..500] : ex.Message;

        try
        {
            var headers = new Dictionary<string, object> { [RetryCountHeader] = retryCount + 1 };

            if (toDeadLetter)
            {
                headers["x-error"] = error;
                headers["x-original-type"] = type;
                headers["x-failed-at"] = DateTime.UtcNow.ToString("O");
                headers["x-reason"] = permanent ? "permanent-failure" : "retries-exhausted";
            }

            var props = _channel!.CreateBasicProperties();
            props.Persistent = true;
            props.MessageId = ea.BasicProperties.MessageId;
            props.Type = ea.BasicProperties.Type;
            props.ContentType = ea.BasicProperties.ContentType;
            props.Timestamp = ea.BasicProperties.Timestamp;
            props.Headers = headers;

            if (toDeadLetter)
            {
                _channel.BasicPublish(RabbitTopology.DeadLetterExchange(_opts), "", props, ea.Body);
                _logger.LogError(ex, "Mensaje {MessageId} enviado a la DLQ ({Reason}) tras {Attempts} intento(s)",
                    ea.BasicProperties.MessageId, permanent ? "falla permanente" : "reintentos agotados", retryCount + 1);
            }
            else
            {
                // Exchange por defecto: la routing key es el nombre de la cola de retry.
                _channel.BasicPublish("", RabbitTopology.RetryQueue(_opts), props, ea.Body);
                _logger.LogWarning(ex, "Mensaje {MessageId} falló (intento {Attempt}); reintento en {Delay} ms",
                    ea.BasicProperties.MessageId, retryCount + 1, _opts.RetryDelayMs);
            }

            // Primero confirmamos que el broker guardó la copia, recién después hacemos ack del original.
            _channel.WaitForConfirmsOrDie(TimeSpan.FromSeconds(5));
            _channel.BasicAck(ea.DeliveryTag, multiple: false);
        }
        catch (Exception publishEx)
        {
            // No pudimos mover el mensaje: lo devolvemos a la cola para no perderlo.
            _logger.LogError(publishEx, "No se pudo enviar {MessageId} a retry/DLQ; se devuelve a la cola",
                ea.BasicProperties.MessageId);
            try { _channel!.BasicNack(ea.DeliveryTag, multiple: false, requeue: true); }
            catch (Exception nackEx) { _logger.LogError(nackEx, "Falló el nack"); }
        }
    }

    private static int GetRetryCount(IBasicProperties props)
    {
        if (props.Headers is null || !props.Headers.TryGetValue(RetryCountHeader, out var value)) return 0;
        return value switch { int i => i, long l => (int)l, _ => 0 };
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}

