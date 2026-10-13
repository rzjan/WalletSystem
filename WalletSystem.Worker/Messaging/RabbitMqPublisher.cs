using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using WalletSystem.Worker.Consumer;
using WalletSystem.Worker.Outbox;

namespace WalletSystem.Worker.Messaging;

public sealed class RabbitMqPublisher : IDisposable
{
    private readonly RabbitMqOptions _options;
    private readonly ConsumerOptions _consumerOptions;
    private readonly object _lock = new object();
    private IConnection? _connection;
    private IModel? _channel;

    public RabbitMqPublisher(IOptions<RabbitMqOptions> options, IOptions<ConsumerOptions> consumerOptions)
    {
        _options = options.Value;
        _consumerOptions = consumerOptions.Value;
    }

    //Publica todo el lote y esperaa UNA sola confirmación del broker.
    // Si lanza excepcion, el llamador no marca nada como procesado (at-least-once).

    public void PublishBatch(IReadOnlyList<OutboxRow> rows)
    {
        lock (_lock)
        {
            var channel = GetChannel();

            foreach (var row in rows)
            {
                var props = channel.CreateBasicProperties();
                props.Persistent = true;

            }
            if (!_channel.WaitForConfirms())
            {
                throw new Exception("Failed to publish messages to RabbitMQ.");
            }
        }
    }
    private static string RoutingKeyFor(string type) => type[(type.LastIndexOf('.') + 1)..];

    private IModel GetChannel()
    {
        if (_channel != null)
            return _channel;

        if (_channel is { IsOpen: true }) return _channel;
        _channel?.Dispose();
        _connection?.Dispose();
        
        _connection = _options.ToConnectionFactory().CreateConnection("wallet-outbox-publisher");        
        _channel = _connection.CreateModel();
        _channel.ConfirmSelect(); // Activa publisher confirms

        RabbitTopology.Declare(_channel, _options, _consumerOptions);
        return _channel;
    }
    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
}
