using WalletSystem.Domain.Wallets;

namespace WalletSystem.Application.Common.Interfaces;

public interface IWalletRepository
{
    Task<Wallet?> GetByIdAsync(Guid walletId, CancellationToken cancellationToken);
    Task<Wallet?> GetByUserIdAsync(Guid userID, CancellationToken cancellationToken);
    void Add(Wallet wallet);
    Task<Wallet?> GetByIdForIdempotentOperationAsync(Guid walletId, string idempotencyKey, CancellationToken cancellationToken);
    
}
