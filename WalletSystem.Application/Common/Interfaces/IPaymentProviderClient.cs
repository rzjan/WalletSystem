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
public record PaymentProviderResult(bool IsSuccess, string? ExternalTransactionId, string? ErrorMessage);
