using WalletSystem.Domain.Common;

namespace WalletSystem.Domain.Bets.Events;

public sealed record BetPlacedEvent(
        Guid BetId,
        Guid WalletId,
        decimal Stake,
        string Currency,
        decimal Odds
    ) : DomainEvent;
