using MediatR;
using WalletSystem.Application.Common.Interfaces;

namespace WalletSystem.Application.Bets.Queries.GetBetById;

public class GetBetByIdQueryHanlder: IRequestHandler<GetBetByIdQuery, GetBetByIdQueryResult>
{
    readonly IBetRepository _betRepository;

    public GetBetByIdQueryHanlder(IBetRepository betRepository)
    {
        _betRepository = betRepository;
    }

    public async Task<GetBetByIdQueryResult> Handle(GetBetByIdQuery request, CancellationToken cancellationToken)
    {
        var bet = await _betRepository.GetByIdAsync(request.BetId, cancellationToken)
            ?? throw new KeyNotFoundException($"Bet {request.BetId} no encontrada.");

        return new GetBetByIdQueryResult(
            bet.Id,
            bet.WalletID,
            bet.Stake.Amount,
            bet.Odds.Value,
            bet.Status.ToString(),
            bet.Payout?.Amount,
            bet.PlacedAt,
            bet.SettledAt
            );

    }
}
