using MediatR;
using WalletSystem.Application.Common.Interfaces;

namespace WalletSystem.Application.Wallets.Queries.GetWalletTransactions;

public class GetWalletTransactionsQueryHandler:IRequestHandler<GetWalletTransactionsQuery, List<WalletTransactionDto>>
{
    private readonly IWalletRepository _walletRepository;

    public GetWalletTransactionsQueryHandler(IWalletRepository walletRepository)
    {
        _walletRepository = walletRepository;
    }

    public async Task<List<WalletTransactionDto>> Handle(GetWalletTransactionsQuery request, CancellationToken cancellationToken)
    {
        var wallet = await _walletRepository.GetByIdAsync(request.WalletId, cancellationToken)
            ?? throw new KeyNotFoundException($"Wallet {request.WalletId} no encontrada.");

        return wallet.Transactions
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new WalletTransactionDto(
                t.Id,
                t.Amount.Amount,
                t.Amount.Currency,
                t.Type.ToString(),
                t.Status.ToString(),
                t.CreatedAt,
                t.CompletedAt))
            .ToList();
    }
}

