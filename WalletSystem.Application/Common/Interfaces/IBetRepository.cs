using WalletSystem.Domain.Bets;

namespace WalletSystem.Application.Common.Interfaces;

public interface IBetRepository
{
    Task<Bet?> GetByIdAsync(Guid betId, CancellationToken cancellationToken);    
    Task<Bet?> GetByIdempotencyKeyAsync(Guid walletId, string idempotencyKey, CancellationToken cancellationToken);
    void Add(Bet bet);
}
