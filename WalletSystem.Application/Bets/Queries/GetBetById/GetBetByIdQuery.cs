using MediatR;

namespace WalletSystem.Application.Bets.Queries.GetBetById;


public record GetBetByIdQuery(Guid BetId):IRequest<GetBetByIdQueryResult>;
public record GetBetByIdQueryResult(
    Guid Id,
    Guid WalletId,
    decimal Stake,
    decimal Odds,
    string Status,
    decimal? Payout,
    DateTime PLacedAt,
    DateTime? SettledAt
    );

