using WalletSystem.Domain.Common;

namespace WalletSystem.Domain.Wallets.Events;

public sealed record WalletDepositRequestedEvent(
    Guid TransactionId,
    Guid WalletId,
    decimal Amount,
    string Currency):DomainEvent;

