using Microsoft.EntityFrameworkCore;
using WalletSystem.Application.Common.Interfaces;
using WalletSystem.Domain.Wallets;

namespace WalletSystem.Infrastructure.Persistence.Repositories;

public class WalletRepository : IWalletRepository
{
    private readonly WalletDbContext _context;

    public WalletRepository(WalletDbContext context)
    {
        _context = context;
    }    

    public async Task<Wallet?> GetByIdAsync(Guid walletId, CancellationToken cancellationToken)
    {
        return await _context.Wallets
            .Include(w=>w.Transactions.OrderByDescending(t=>t.CreatedAt).Take(50))
            .FirstOrDefaultAsync(w=>w.Id == walletId, cancellationToken);
    }

    public async Task<Wallet?> GetByUserIdAsync(Guid userID, CancellationToken cancellationToken)
    {
        return await _context.Wallets
             .Include(w => w.Transactions.OrderByDescending(t => t.CreatedAt).Take(50))
             .FirstOrDefaultAsync(w => w.UserId == userID, cancellationToken);
    }

    public void Add(Wallet wallet)=> _context.Wallets .Add(wallet);

    public async Task<Wallet?> GetByIdForIdempotentOperationAsync(Guid walletId, string idempotencyKey, CancellationToken cancellationToken)
    {
        return await _context.Wallets
               .Include(w => w.Transactions.Where(t => t.IdempotencyKey == idempotencyKey))
               .FirstOrDefaultAsync(w => w.Id == walletId);
    }
}
