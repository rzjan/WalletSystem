using WalletSystem.Application.Common.Models;
using WalletSystem.Application.Wallets.Queries.GetWalletBalance;
using WalletSystem.Application.Wallets.Queries.GetWalletTransactions;

namespace WalletSystem.Application.Common.Interfaces;

public interface IWalletReadService
{
    Task<GetWalletBalanceResult?> GetBalanceAsync(Guid walletId, CancellationToken cancellationToken);
    Task<PagedResult<WalletTransactionDto>> GetTransactionsAsync(Guid walletId, int page, int pageSize, CancellationToken cancellationToken);
}
