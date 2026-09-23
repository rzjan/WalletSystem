namespace WalletSystem.Domain.Common;

public abstract record DomainEvent
{
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
