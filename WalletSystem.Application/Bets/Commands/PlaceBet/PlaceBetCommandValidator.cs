using FluentValidation;

namespace WalletSystem.Application.Bets.Commands.PlaceBet;

public class PlaceBetCommandValidator:AbstractValidator<PlaceBetCommand>
{
    public PlaceBetCommandValidator()
    {
        RuleFor(x=> x.WalletId)
            .NotEmpty()
            .WithMessage("El WalletID es requerido.");

        RuleFor(x=> x.Stake)
            .GreaterThan(0)
            .WithMessage("El monto apostado debe ser mayor a cero.");

        RuleFor(x=> x.Currency)
            .NotEmpty()
            .Length(3)
            .WithMessage("La moneda debe ser un código ISO de 3 letras.");

        RuleFor(x=> x.Odds)
            .GreaterThan(0)
            .WithMessage("La cuota debe ser mayor a cero.");

        RuleFor(x=> x.IdempotencyKey)
            .NotEmpty()
            .WithMessage("La clave de idempotencia es requerida.");
    }
}
