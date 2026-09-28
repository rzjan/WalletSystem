using WalletSystem.Application.Bets.Queries.GetBetById;

namespace WalletSystem.Application.Common.Interfaces;

public interface IBetReadService
{
    Task<GetBetByIdQueryResult?> GetByIdAsync(Guid betId, CancellationToken cancellationToken);
}
