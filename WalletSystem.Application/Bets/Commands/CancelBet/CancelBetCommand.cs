using MediatR;

namespace WalletSystem.Application.Bets.Commands.CancelBet;

public record CancelBetCommand(Guid BetId) : IRequest<CancelBetResult>;

public record CancelBetResult(Guid BetId, string Status, decimal RefundedAmount, string Currency);
