using WalletSystem.Domain.Common;

namespace WalletSystem.Domain.Bets.Events;

public sealed record BetSettledEvent(
    Guid BetId,
    Guid WalletId,
    BetStatus Result,
    decimal PayoutAmount,
    string Currency
    ):DomainEvent;