namespace WalletSystem.Worker.Consumer;

public sealed class ConsumerOptions
{
    public string QueueName { get; set; } = "wallet.events.consumer";
    public ushort Prefetch { get; set; } = 10;
    public int MaxRetries { get; set; } = 3;
    public int RetryDelayMs { get; set; } = 5000;
    public bool FaultInjectionEnabled { get; set; }
}
