using FluentValidation;

namespace WalletSystem.Application.Bets.Commands.SettleBet;

public class SettleBetCommandValidator:AbstractValidator<SettleBetCommand>
{
    public SettleBetCommandValidator()
    {
        RuleFor(x => x.BetId)
            .NotEmpty()
            .WithMessage("El BetId es requerido.");

        RuleFor(x => x.OutCome)
            .IsInEnum()
            .WithMessage("El resultado debe ser Won o Lost.");
    }
}
