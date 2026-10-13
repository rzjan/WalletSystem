using System.Text.Json;
using Microsoft.Extensions.Options;

namespace WalletSystem.Worker.Consumer;

// SOLO para aprender: provoca fallas leyendo el payload. Se activa únicamente en Development.
//   {"fault":"poison"}                              falla siempre (agota reintentos -> DLQ)
//   {"fault":"permanent"}                           falla permanente (directo a la DLQ)
//   {"fault":"flaky","failUntilAttempt":3}          falla hasta el intento 2 y funciona en el 3
//   {"delayMs":3000}                                simula un consumer lento
public sealed class FaultInjector
{
    private readonly bool _enabled;

    public FaultInjector(IOptions<ConsumerOptions> options) => _enabled = options.Value.FaultInjectionEnabled;

    public async Task ApplyAsync(EventContext context, CancellationToken ct)
    {
        if (!_enabled) return;

        JsonDocument doc;
        try { doc = JsonDocument.Parse(context.Payload); }
        catch (JsonException) { return; }

        using (doc)
        {
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return;

            if (root.TryGetProperty("delayMs", out var delay) && delay.TryGetInt32(out var ms))
                await Task.Delay(ms, ct);

            if (!root.TryGetProperty("fault", out var fault) || fault.ValueKind != JsonValueKind.String) return;

            switch (fault.GetString())
            {
                case "permanent":
                    throw new PermanentFailureException("Falla permanente inyectada");
                case "poison":
                    throw new InvalidOperationException("Falla inyectada: este mensaje siempre falla");
                case "flaky":
                    var until = root.TryGetProperty("failUntilAttempt", out var u) && u.TryGetInt32(out var n) ? n : 2;
                    if (context.Attempt < until)
                        throw new InvalidOperationException($"Falla transitoria inyectada (intento {context.Attempt})");
                    break;
            }
        }
    }
}
