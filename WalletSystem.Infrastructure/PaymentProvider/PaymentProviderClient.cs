using Polly.Bulkhead;
using Polly.CircuitBreaker;
using Polly.Timeout;
using System.Net.Http.Json;
using WalletSystem.Application.Common.Interfaces;

namespace WalletSystem.Infrastructure.PaymentProvider;

public class PaymentProviderClient: IPaymentProviderClient
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
            Content = JsonContent.Create( new{ amount, currency })
        };
        request.Headers.Add("Idempotency-Key", idempotencyKey);

        try
        {
            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadFromJsonAsync<ChargeResponseDto>(cancellationToken);
                return new PaymentProviderResult(true, body?.TransactionId, null);
            }

            //Respuesta de negocio del proveedor (rechazo), no un fallo de infraestructura. Se puede manejar como un resultado esperado.
            var error = await response.Content.ReadFromJsonAsync<ChargeErrorDto>(cancellationToken);
            return new PaymentProviderResult(false, null, error?.Message?? $"El proveedor respondió {(int)response.StatusCode}.");
        }
        catch (BrokenCircuitException)
        {

            return new PaymentProviderResult(false, null, "El proveedor de pagos no está disponible en este momento.");
            ;
        }
        catch (BulkheadRejectedException)
        {

            return new PaymentProviderResult(false, null, "Demasiadas operaciones concurrentes hacia el proveedor de pagos.");
        }
        catch (TimeoutRejectedException)
        {

            return new PaymentProviderResult(false, null, "El proveedor de pagos no respondió a tiempo.");

        }

    }

    private sealed record ChargeResponseDto(string TransactionId);
    private sealed record ChargeErrorDto(string Message);
}
