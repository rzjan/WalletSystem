namespace WalletSystem.Application.Common.Interfaces;

public interface IPaymentProviderClient
{
    Task<PaymentProviderResult> ChargeAsync(
        decimal amount,
        string currency,
        string idempotencyKey,
        CancellationToken cancellationToken
        );
}


public enum PaymentOutcome
{
    Approved = 1,       // el proveedor confirmó el cobro
    Declined = 2,       // el proveedor respondió un rechazo definitivo: no se cobró
    Indeterminate = 3   // no sabemos qué pasó (timeout, 5xx, circuito abierto): no se decide
}

public record PaymentProviderResult(
        PaymentOutcome Outcome,
        string? ExternalTransactionId,
        string? ErrorMessage
    )
{
    public bool IsSuccess => Outcome == PaymentOutcome.Approved;

    public static PaymentProviderResult Approved(string? externalTransactionId) =>
        new(PaymentOutcome.Approved, externalTransactionId, null);

    public static PaymentProviderResult Declined(string? reason) =>
        new(PaymentOutcome.Declined,null, reason);

    public static PaymentProviderResult Indeterminate(string? reason) =>
        new(PaymentOutcome.Indeterminate,null, reason);
}


