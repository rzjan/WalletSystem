using WalletSystem.Domain.Common;

namespace WalletSystem.Domain.Bets;

public class Odds : ValueObject
{
    public decimal Value { get; }

    private const decimal MinValue = 1.01m;
    private const decimal MaxValue = 1000m;


    private Odds(decimal value)
    {
        Value = value;
    }

    public static Result<Odds> Create(decimal value)
    {
        if (value < MinValue)
            return Result.Failure<Odds>($"La cuota debe ser mayor o igual a {MinValue}.");

        if (value > MaxValue)
            return Result.Failure<Odds>($"La cuota no puede superar {MaxValue}.");

        return Result.Success(new Odds(value));
    }

    public Money CaculatePayout(Money stake) =>
        Money.Create(stake.Amount * Value, stake.Currency).Value;
    

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString("N2");
}
