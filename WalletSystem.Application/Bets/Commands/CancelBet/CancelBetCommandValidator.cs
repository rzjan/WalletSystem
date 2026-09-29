using FluentValidation;

namespace WalletSystem.Application.Bets.Commands.CancelBet;

public class CancelBetCommandValidator : AbstractValidator<CancelBetCommand>
{
    public CancelBetCommandValidator()
    {
        RuleFor(x => x.BetId).NotEmpty().WithMessage("El BetId es requerido.");
    }
}