using Microsoft.Extensions.Logging;
using Polly;
using Polly.Timeout;

namespace WalletSystem.Infrastructure.PaymentProvider;

public static class ResiliencePolicies
{
    public static IAsyncPolicy<HttpResponseMessage> GetCombinedPolicy(ILogger logger)
    {
        var timeout = Policy.TimeoutAsync<HttpResponseMessage>(
            TimeSpan.FromSeconds(5), TimeoutStrategy.Optimistic
            );

        var bulkhead = Policy.BulkheadAsync<HttpResponseMessage>(
            maxParallelization: 10, maxQueuingActions: 20);

        var circuitBreaker = Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .Or<TimeoutRejectedException>()
            .OrResult(r => (int)r.StatusCode >= 500)
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (outcome, breakDelay) => logger.LogWarning(
                    "Circuit breaker abierto por {Second}s. Motivo: {Reason}",
                    breakDelay.TotalSeconds, outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString()),
                onReset: () => logger.LogInformation("Circuit breaker cerrado, proveedor recuperado."),
                onHalfOpen: () => logger.LogInformation("Circuit breaker en estado Half-Open, probando proveedor.")
            );

        var retry = Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .Or<TimeoutRejectedException>()
            .OrResult(r => (int)r.StatusCode >= 500)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt =>
                        TimeSpan.FromSeconds(Math.Pow(2, attempt)) + TimeSpan.FromMilliseconds(Random.Shared.Next(0, 250)),
                onRetry: (outcome, delay, attempt, _) => logger.LogWarning(
                    "Reintento {Attempt} al proveedor de pagos en {Delay}s", attempt, delay.TotalSeconds));


        // De afuera hacia adentro: Retry envuelve a CircuitBreaker., que envuelve a Bullhead, que envuelve a Timeout.
        return Policy.WrapAsync(retry, circuitBreaker, bulkhead, timeout);
    }
}
