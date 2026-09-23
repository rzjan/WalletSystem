using WalletSystem.Domain.Common;

namespace WalletSystem.Domain.Wallets.Events;

public sealed record WalletTransactionCompletedEvent(
    Guid TransactionId,
    Guid WalletId,
    decimal Amount,
    string Currency,
    TransactionType Type,
    decimal NewBalance) : DomainEvent;