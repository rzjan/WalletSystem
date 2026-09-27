using MediatR;

namespace WalletSystem.Application.Wallets.Commands.Withdraw;

public record WithdrawCommand(
    Guid WalletID,
    decimal Amount,
    string Currency,
    string IdempotencyKey
    ):IRequest<WithDrawResult>;


public record WithDrawResult(Guid TransactionId, decimal NewBalance, string Status);
