using MediatR;

namespace WalletSystem.Application.Wallets.Commands.Deposit;


public record DepositCommand(
    Guid WalletID,
    decimal Amount,
    string Currency,
    string IdempotencyKey) : IRequest<DepositResult>;


public record DepositResult(Guid TransactionId, decimal NewBalance, string Status);
