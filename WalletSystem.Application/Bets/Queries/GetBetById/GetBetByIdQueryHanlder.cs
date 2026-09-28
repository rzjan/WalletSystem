using MediatR;
using WalletSystem.Application.Common.Interfaces;

namespace WalletSystem.Application.Bets.Queries.GetBetById;

public class GetBetByIdQueryHanlder : IRequestHandler<GetBetByIdQuery, GetBetByIdQueryResult>
{

    private readonly IBetReadService _readService;

    public GetBetByIdQueryHanlder(IBetReadService readService)
    {
        _readService = readService;
    }

    public async Task<GetBetByIdQueryResult> Handle(GetBetByIdQuery request, CancellationToken cancellationToken)
    {
        return await _readService.GetByIdAsync(request.BetId, cancellationToken)
            ?? throw new KeyNotFoundException($"Bet {request.BetId} no encontrada.");
    }
}
