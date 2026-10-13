using RabbitMQ.Client;

namespace WalletSystem.Worker.Messaging;

public sealed class RabbitMqOptions
{
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "wallet";
    public string Password { get; set; } = string.Empty;
    public string Exchange { get; set; } = "wallet_events";

    public ConnectionFactory ToConnectionFactory(bool asyncConsumers = false) => new()
    {
        HostName = HostName,
        Port = Port,
        UserName = UserName,
        Password = Password,
        AutomaticRecoveryEnabled = true,
        TopologyRecoveryEnabled = true,
        DispatchConsumersAsync = asyncConsumers //Necesario para AsyncEventingBasicConsumer
    };

}
