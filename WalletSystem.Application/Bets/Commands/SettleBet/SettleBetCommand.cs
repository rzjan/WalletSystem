using MediatR;

namespace WalletSystem.Application.Bets.Commands.SettleBet;

public record SettleBetCommand(
    Guid BetId,
    BetOutcome OutCome
    ):IRequest<SettleBetResult>;

public record SettleBetResult(
    Guid BetId, string Status, decimal PayoutAmount, string Currency
    );
