using FluentValidation;

namespace WalletSystem.Application.Wallets.Queries.GetWalletTransactions;

public class GetWalletTransactionsQueryValidator:AbstractValidator<GetWalletTransactionsQuery>
{
    public GetWalletTransactionsQueryValidator()
    {
        RuleFor(x => x.WalletId)
            .NotEmpty();

        RuleFor(x=> x.Page)
            .GreaterThanOrEqualTo(1);

        RuleFor(x=> x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("El tamaño de página debe estar entre 1 y 100.");

    }
}
