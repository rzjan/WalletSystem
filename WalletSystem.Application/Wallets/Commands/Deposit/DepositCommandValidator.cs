using FluentValidation;

namespace WalletSystem.Application.Wallets.Commands.Deposit;

public class DepositCommandValidator: AbstractValidator<DepositCommand>
{
    public DepositCommandValidator()
    {
        RuleFor(x => x.WalletID)
            .NotEmpty().WithMessage("El wallet es requerido.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("El monto debe ser mayor a cero.");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .Length(3).WithMessage("La moneda debe ser un código ISO de 3 letras.");

        RuleFor(x=> x.IdempotencyKey)
            .NotEmpty().WithMessage("La clave de idempotencia es requerida.");

    }

}
