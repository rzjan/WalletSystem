using MediatR;

namespace WalletSystem.Application.Wallets.Queries.GetWalletBalance;

public record GetWalletBalanceQuery(
    Guid WalletId):IRequest<GetWalletBalanceResult>;

public record GetWalletBalanceResult(Guid WalletId, decimal Balance, string Currency);

