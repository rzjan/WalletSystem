using Polly.Bulkhead;
using Polly.CircuitBreaker;
using Polly.Timeout;
using System.Net.Http.Json;
using System.Text.Json;
using WalletSystem.Application.Common.Interfaces;

namespace WalletSystem.Infrastructure.PaymentProvider;

public class PaymentProviderClient : IPaymentProviderClient
{
    private readonly HttpClient _httpClient;

    public PaymentProviderClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<PaymentProviderResult> ChargeAsync(
            decimal amount, string currency,
            string idempotencyKey, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "charges")
        {
            Content = JsonContent.Create(new { amount, currency })
        };
        request.Headers.Add("Idempotency-Key", idempotencyKey);

        try
        {
            var response = await _httpClient.SendAsync(request, cancellationToken);
            var status = (int)response.StatusCode;

            if (response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadFromJsonAsync<ChargeResponseDto>(cancellationToken);
                return PaymentProviderResult.Approved(body?.TransactionId);
            }


            //5xx tras agotar reintentos, o 408/429: no es una decisión de negocio del proveedor
            if (status >= 500 || status is 408 or 429)
                return PaymentProviderResult.Indeterminate($"El proveedor respondió {status} y no se pudo confirmar el cobro.");

            //Resto de 4xx: rechazo definitivo del proveedor, no se cobró
            var message = await TryReadErrorMessageAsync(response, cancellationToken);
            return PaymentProviderResult.Declined(message ?? $"El proveedor rechazo la operación ({status}).");
        }
        catch (Exception ex) when (
                ex is BrokenCircuitException or BulkheadRejectedException
                or TimeoutRejectedException or HttpRequestException)
        {
            // Pudo haber intentos previos que si llegaron al proveedor: no podemos asumir que no se cobró.
            return PaymentProviderResult.Indeterminate($"No se pudo confirmar el cobro con el proveedor: {ex.GetType().Name}.");
        }
    }

    private sealed record ChargeResponseDto(string TransactionId);
    private sealed record ChargeErrorDto(string Message);

    private static async Task<string?> TryReadErrorMessageAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            return (await response.Content.ReadFromJsonAsync<ChargeErrorDto>(ct))?.Message;
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            return null; // el cuerpo no era el JSON esperado; no es motivo para romper el flujo
        }
    }
}
