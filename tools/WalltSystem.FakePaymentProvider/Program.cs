using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var charges = new ConcurrentDictionary<string, ChargeRecord>();
var chaos = new ChaosSettings();

app.MapPost("/charges", async (HttpRequest request, ChargeRequest body, CancellationToken ct) =>
{
    if (!request.Headers.TryGetValue("Idempotency-Key", out var keyValues) || string.IsNullOrWhiteSpace(keyValues))
        return Results.BadRequest(new { message = "Falta el header Idempotency-Key." });

    var key = keyValues.ToString();

    // Replay: misma clave, misma respuesta que la primera vez, sin volver a cobrar
    if (charges.TryGetValue(key, out var existing))
        return ToResult(existing);

    if (Roll(chaos.SlowRate))
        await Task.Delay(TimeSpan.FromSeconds(7), ct); // más que el timeout de Polly (5s)

    if (Roll(chaos.ErrorRate))
        return Results.Json(new { message = "Servicio no disponible (simulado)." }, statusCode: 503);

    var record = body.Amount > 100_000
        ? new ChargeRecord(false, null, "Fondos insuficientes en el medio de pago.")
        : new ChargeRecord(true, $"ext-{Guid.NewGuid():N}", null);

    var stored = charges.GetOrAdd(key, record); // el cobro queda hecho; con requests concurrentes gana uno solo

    // El caso peligroso: el cobro YA se procesó pero la respuesta no llega al cliente
    if (Roll(chaos.ResponseLossRate))
        return Results.Json(new { message = "Se perdió la respuesta (simulado)." }, statusCode: 503);

    return ToResult(stored);
});

// Para la futura reconciliación: consultar el estado de un cobro por su clave
app.MapGet("/charges/{key}", (string key) =>
    charges.TryGetValue(key, out var record) ? ToResult(record) : Results.NotFound());

// Cambiar el caos en caliente, sin reiniciar
app.MapGet("/admin/chaos", () => chaos);
app.MapPost("/admin/chaos", (ChaosSettings settings) =>
{
    chaos.ErrorRate = settings.ErrorRate;
    chaos.ResponseLossRate = settings.ResponseLossRate;
    chaos.SlowRate = settings.SlowRate;
    return Results.Ok(chaos);
});

app.Run();

static bool Roll(double rate) => Random.Shared.NextDouble() < rate;

static IResult ToResult(ChargeRecord record) => record.Approved
    ? Results.Ok(new { transactionId = record.TransactionId })
    : Results.Json(new { message = record.Message }, statusCode: 402);

record ChargeRequest(decimal Amount, string Currency);
record ChargeRecord(bool Approved, string? TransactionId, string? Message);

class ChaosSettings
{
    public double ErrorRate { get; set; }          // 503 antes de procesar
    public double ResponseLossRate { get; set; }   // procesa el cobro y después responde 503
    public double SlowRate { get; set; }           // tarda 7s: dispara el timeout de Polly
}