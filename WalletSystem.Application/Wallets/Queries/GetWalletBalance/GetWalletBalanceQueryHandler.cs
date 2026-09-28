using MediatR;
using WalletSystem.Application.Common.Interfaces;

namespace WalletSystem.Application.Wallets.Queries.GetWalletBalance;

public class GetWalletBalanceQueryHandler : IRequestHandler<GetWalletBalanceQuery, GetWalletBalanceResult>
{
    private readonly IWalletReadService _readService;

    public GetWalletBalanceQueryHandler(IWalletReadService readService)
    {
        _readService = readService;
    }

    public async Task<GetWalletBalanceResult> Handle(GetWalletBalanceQuery request, CancellationToken cancellationToken)
    {
        return await _readService.GetBalanceAsync(request.WalletId, cancellationToken)
            ?? throw new KeyNotFoundException($"Wallet {request.WalletId} no encontrada.");        
    }
}
