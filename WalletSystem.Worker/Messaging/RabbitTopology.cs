using WalletSystem.Worker.Consumer;
using RabbitMQ.Client;

namespace WalletSystem.Worker.Messaging;

// Declara exchange, colas, retry y DLQ. Lo llaman el publisher Y el consumer:
// si el publisher corre primero y la cola no existe, el exchange descarta los mensajes.
public static class RabbitTopology
{
    public static string RetryQueue(ConsumerOptions c) => $"{c.QueueName}.retry";
    public static string DeadLetterQueue(ConsumerOptions c) => $"{c.QueueName}.dlq";
    public static string DeadLetterExchange(ConsumerOptions c) => $"{c.QueueName}.dlx";

    public static void Declare(IModel channel, RabbitMqOptions mq, ConsumerOptions c)
    {
        channel.ExchangeDeclare(mq.Exchange, ExchangeType.Topic, durable: true);

        // DLQ: exchange fanout -> cola
        channel.ExchangeDeclare(DeadLetterExchange(c), ExchangeType.Fanout, durable: true);
        channel.QueueDeclare(DeadLetterQueue(c), durable: true, exclusive: false, autoDelete: false);
        channel.QueueBind(DeadLetterQueue(c), DeadLetterExchange(c), routingKey: "");

        // Retry: el mensaje espera RetryDelayMs y el broker lo devuelve a la cola principal
        // (dead-letter hacia el exchange por defecto, con routing key = nombre de la cola).
        channel.QueueDeclare(RetryQueue(c), durable: true, exclusive: false, autoDelete: false,
            arguments: new Dictionary<string, object>
            {
                ["x-message-ttl"] = c.RetryDelayMs,
                ["x-dead-letter-exchange"] = "",
                ["x-dead-letter-routing-key"] = c.QueueName
            });

        // Cola principal. El DLX es una red de seguridad si algún día hacemos BasicNack(requeue: false).
        channel.QueueDeclare(c.QueueName, durable: true, exclusive: false, autoDelete: false,
            arguments: new Dictionary<string, object>
            {
                ["x-dead-letter-exchange"] = DeadLetterExchange(c)
            });
        channel.QueueBind(c.QueueName, mq.Exchange, routingKey: "#");
    }
}
