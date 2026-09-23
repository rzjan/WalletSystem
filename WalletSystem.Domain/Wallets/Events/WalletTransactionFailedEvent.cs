using WalletSystem.Domain.Common;

namespace WalletSystem.Domain.Wallets.Events;

public sealed record WalletTransactionFailedEvent(
    Guid TransactionId,
    Guid WalletId,
    string Reason
    ):DomainEvent;
