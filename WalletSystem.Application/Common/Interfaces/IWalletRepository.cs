using WalletSystem.Domain.Wallets;

namespace WalletSystem.Application.Common.Interfaces;

public interface IWalletRepository
{
    Task<Wallet?> GetByIdAsync(Guid walletId, CancellationToken cancellation);
    Task<Wallet?> GetByUserIdAsync(Guid userID, CancellationToken cancellation);
    void Add(Wallet wallet);
}
