using MediatR;

namespace WalletSystem.Application.Bets.Commands.PlaceBet;

public record PlaceBetCommand
(
    Guid WalletId,
    decimal Stake,
    string Currency,
    decimal Odds,
    string IdempotencyKey): IRequest<PlaceBetResult>;


public record PlaceBetResult(Guid BetId, decimal NewWalletBalance, string Status);