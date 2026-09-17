using WalletSystem.Domain.Common;

namespace WalletSystem.Domain.Wallets;

public class WalletTransaction
{
    public Guid Id { get; private set; }
    public Guid WalletId { get; private set; }
    public Money Amount { get; private set; }
    public TransactionType Type { get; private set; }
    public TransactionStatus Status { get; private set; }
    public string IdempotencyKey { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public string? FailureReason { get; private set; }

    //Constructor sin parametros para EF Core
    private WalletTransaction() { }

    private WalletTransaction(
        Guid walletId,
        Money amount,
        TransactionType type,
        string idempotencyKey)
    {
        Id = Guid.NewGuid();
        WalletId = walletId;
        Amount = amount;
        Type = type;
        Status = TransactionStatus.Pending;
        IdempotencyKey = idempotencyKey;
        CreatedAt = DateTime.UtcNow;
    }

    public static Result<WalletTransaction> Create(
            Guid walletId,
            Money amount,
            TransactionType type,
            string idempotencyKey)
    {
        if (walletId == Guid.Empty)
            return Result.Failure<WalletTransaction>("El walletId es requerido.");

        if (amount.Amount <= 0)
            return Result.Failure<WalletTransaction>("El monto de la transacción debe ser mayor a cero.");

        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return Result.Failure<WalletTransaction>("La clave de idempotenciaes requerida.");

        return Result.Success(new WalletTransaction(walletId, amount, type, idempotencyKey));
    }

    internal void MarkAsCompleted()
    {
        if (Status != TransactionStatus.Pending)
            throw new InvalidOperationException(
                $"Solo se puede completar una transacción en estado Pending. Estado actual: {Status}."
                );

        Status = TransactionStatus.Completed;
        CompletedAt = DateTime.UtcNow;
    }

    internal void MarkAsFailed(string reason)
    {
        if (Status != TransactionStatus.Pending)
            throw new InvalidOperationException(
                $"Solo se puede fallar una transacción en estado Pending. Estado actual: {Status}."
                );

        Status = TransactionStatus.Failed;
        FailureReason = reason;
        CompletedAt = DateTime.UtcNow;
    }   
}
