using WalletSystem.Domain.Bets;

namespace WalletSystem.Application.Common.Interfaces;

public interface IBetRepository
{
    Task<Bet?> GetByIdAsync(Guid betId, CancellationToken cancellation);
    void Add(Bet bet);
}
