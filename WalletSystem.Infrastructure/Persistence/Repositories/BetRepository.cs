using Microsoft.EntityFrameworkCore;
using WalletSystem.Application.Common.Interfaces;
using WalletSystem.Domain.Bets;

namespace WalletSystem.Infrastructure.Persistence.Repositories;

public class BetRepository : IBetRepository
{
    private readonly WalletDbContext _context;

    public BetRepository(WalletDbContext context)
    {
        _context = context;
    }

    public Task<Bet?> GetByIdAsync(Guid betId, CancellationToken cancellationToken)
    {
        return _context.Bets.FirstOrDefaultAsync(b => b.Id == betId, cancellationToken);
    }

    public async Task<Bet?> GetByIdempotencyKeyAsync(Guid walletId, string idempotencyKey, CancellationToken cancellationToken)
    {
        return await _context.Bets.FirstOrDefaultAsync(
            b => b.Id == walletId && 
            b.IdempotencyKey == idempotencyKey,
            cancellationToken
            );
    }

    public void Add(Bet bet) => _context.Add(bet);
}
