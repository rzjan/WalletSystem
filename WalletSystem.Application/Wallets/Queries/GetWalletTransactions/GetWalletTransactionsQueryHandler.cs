using MediatR;
using WalletSystem.Application.Common.Interfaces;
using WalletSystem.Application.Common.Models;

namespace WalletSystem.Application.Wallets.Queries.GetWalletTransactions;

public class GetWalletTransactionsQueryHandler
    :IRequestHandler<GetWalletTransactionsQuery, PagedResult<WalletTransactionDto>>
{
    private readonly IWalletReadService _readService;

    public GetWalletTransactionsQueryHandler(IWalletReadService readService)
    {
        _readService = readService;
    }

    public async Task<PagedResult<WalletTransactionDto>> Handle(GetWalletTransactionsQuery request, CancellationToken cancellationToken)
    {
        return await _readService.GetTransactionsAsync(
            request.WalletId,
            request.Page,
            request.PageSize,
            cancellationToken);
            
    }
}

