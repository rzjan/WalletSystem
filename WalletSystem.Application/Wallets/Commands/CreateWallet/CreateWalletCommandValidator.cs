using FluentValidation;

namespace WalletSystem.Application.Wallets.Commands.CreateWallet;

public class CreateWalletCommandValidator : AbstractValidator<CreateWalletCommand>
{
    public CreateWalletCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("El UserId es requerido.");
        RuleFor(x => x.Currency)
            .NotEmpty()
            .Length(3)
            .WithMessage("La moneda debe ser un código ISO de 3 letras.");

    }
}
