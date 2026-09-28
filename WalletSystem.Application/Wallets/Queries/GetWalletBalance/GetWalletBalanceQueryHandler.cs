using MediatR;
using Microsoft.IdentityModel.Tokens.Experimental;
using WalletSystem.Application.Common.Interfaces;

namespace WalletSystem.Application.Wallets.Queries.GetWalletBalance;

public class GetWalletBalanceQueryHandler:IRequestHandler<GetWalletBalanceQuery, GetWalletBalanceResult>
{
    private readonly IWalletRepository _walletRepository;

    public GetWalletBalanceQueryHandler(IWalletRepository walletRepository)
    {
        _walletRepository = walletRepository;
    }

    public async Task<GetWalletBalanceResult> Handle(GetWalletBalanceQuery request, CancellationToken cancellationToken)
    {
        var wallet = await _walletRepository.GetByIdAsync(request.WalletId, cancellationToken)
            ?? throw new KeyNotFoundException($"Wallet {request.WalletId} no encontrada.");
        return new GetWalletBalanceResult(wallet.Id, wallet.Balance.Amount, wallet.Balance.Currency);
    }
}
