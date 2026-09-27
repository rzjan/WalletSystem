using WalletSystem.Domain.Bets;

namespace WalletSystem.Application.Common.Interfaces;

public interface IBetRepository
{
    Task<Bet?> GetByIdAsync(Guid betId, CancellationToken cancellation);
    Task<Bet?> GetIdempotencyKey(string idempotencyKey, CancellationToken cancellationToken);
    void Add(Bet bet);
}
