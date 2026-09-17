namespace WalletSystem.Domain.Common;

public class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }


    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Result<Money> Create(decimal amount, string currency)
    {
        if (amount < 0)
            return Result.Failure<Money>("El monto no puede ser negativo");

        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
           return Result.Failure<Money>("La moneda debe ser un código ISO de 3 letras (ej. USD, ARS).");

        return Result.Success(new Money(amount, currency.ToUpperInvariant()));
    }

    public static Money Zero(string currency) => new Money(0, currency.ToUpperInvariant());

    public Money Add(Money other) 
    {
        EnsureSameCurrency(other);
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Substract(Money other) 
    {
        EnsureSameCurrency(other);
        return new Money(Amount - other.Amount, Currency);
    }


    
    public bool IsGeaterThanOrEqualTo(Money other)
    {
        EnsureSameCurrency(other);
        return Amount >= other.Amount;
    }

    private void EnsureSameCurrency(Money other) 
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException(
                $"No se pueden operar montos en distinta moneda; {Currency} vs {other.Currency}."
                );
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    public override string ToString() => $"{Amount} {Currency}";
}
