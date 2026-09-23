using WalletSystem.Domain.Common;
using WalletSystem.Domain.Wallets.Events;

namespace WalletSystem.Domain.Wallets;

public class Wallet:Entity
{
    private readonly List<WalletTransaction> _transactions = new ();

    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Money Balance { get; set; }
    public DateTime CreatedAt { get; set; }

    public IReadOnlyCollection<WalletTransaction> Transactions => _transactions.AsReadOnly();

    private Wallet() { }

    private Wallet(Guid userId, string currency)
    {
        Id = userId;
        UserId = userId;
        Balance = Money.Zero(currency);
        CreatedAt = DateTime.UtcNow;
    }

    public static Result<Wallet> Create(Guid userId, string currency) 
    {
        if (userId == Guid.Empty)
            return Result.Failure<Wallet>("El usuario es requerido");

        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
            return Result.Failure<Wallet>("La moneda debe ser un código ISO de 3 letras");

        return Result.Success(new Wallet(userId, currency));
    }

    public Result<WalletTransaction> Deposit(Money amount, string idempotencyKey)
        => ApplyTransaction(amount, TransactionType.Deposit, idempotencyKey);

    public Result<WalletTransaction> Withdraw(Money amount, string idempotencyKey)
        => ApplyTransaction(amount,TransactionType.Withdrawal, idempotencyKey);

    public Result<WalletTransaction> DebitForBet(Money amount, string idempotencyKey)
        => ApplyTransaction(amount, TransactionType.BetPlaced, idempotencyKey);

    public Result<WalletTransaction> CreditPrize(Money amount, string idempotencyKey)
        => ApplyTransaction(amount, TransactionType.PrizeCredited, idempotencyKey);

    private Result<WalletTransaction> ApplyTransaction(
        Money amount,
        TransactionType type,
        string idempotencyKey) 
    {
        // 1) Idempotencia: si ya procesamos esta clave, devolvemos la transacción existente
        var existing = _transactions.FirstOrDefault(t=> t.IdempotencyKey == idempotencyKey);
        if (existing is not null)
            return Result.Success(existing);

        // 2) Creación y validación de la transacción
        var transactionResult = WalletTransaction.Create(Id, amount, type, idempotencyKey);
        if (!transactionResult.IsSuccess)
            return Result.Failure<WalletTransaction>(transactionResult.Error!);

        var transaction = transactionResult.Value;
        _transactions.Add(transaction);

        // 3) Aplicación del efecto sobre el saldo
        var isDebit = type is TransactionType.Withdrawal or TransactionType.BetPlaced;

        if (isDebit)
        {
            if (!Balance.IsGeaterThanOrEqualTo(amount))
            {
                transaction.MarkAsFailed("Fondos insuficientes");

                RaiseDomainEvent(new WalletTransactionFailedEvent(
                        transaction.Id, Id, "Fondos insuficientes."));

                return Result.Failure<WalletTransaction>("Fondos insuficientes");
            }

            Balance = Balance.Substract(amount);
        }
        else
        {
            Balance = Balance.Add(amount);
        }

        transaction.MarkAsCompleted();

        RaiseDomainEvent(new WalletTransactionCompletedEvent(
                    transaction.Id, Id, amount.Amount, amount.Currency, type, Balance.Amount
            ));

        return Result.Success(transaction);
    }
}
