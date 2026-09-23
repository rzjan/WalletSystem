using WalletSystem.Domain.Common;

namespace WalletSystem.Domain.Bets.Events;

public sealed record BetCancelledEvent(
    Guid BetId,
    Guid WalletId
    ) : DomainEvent;

