namespace WalletSystem.Worker.Outbox;

public sealed class OutboxOptions
{
    // Batch size for processing outbox messages
    public int BatchSize { get; set; } = 50;
    // Polling interval in milliseconds
    public int PollIntervalMs { get; set; } = 1000;
}
