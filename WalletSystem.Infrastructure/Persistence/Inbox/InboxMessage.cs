namespace WalletSystem.Infrastructure.Persistence.Inbox;

public sealed class InboxMessage
{
    public string ConsumerNamer { get; set; } = string.Empty;
    public Guid MessageId { get; set; }
    public DateTime ProcessedAt { get; set; }

}
