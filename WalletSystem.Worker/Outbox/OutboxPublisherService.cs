using WalletSystem.Worker.Outbox;
using Dapper;
using global::WalletSystem.Worker.Messaging;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

public sealed class OutboxPublisherService : BackgroundService
{
    // UPDLOCK + READPAST: cada instancia bloquea las filas que toma y las demás las saltean.
    private const string SelectSql = """
        SELECT TOP (@Batch) Id, Type, Payload, OccurredAt
        FROM OutboxMessages WITH (UPDLOCK, READPAST, ROWLOCK)
        WHERE ProcessedAt IS NULL
        ORDER BY OccurredAt
        """;

    private const string MarkProcessedSql = """
        UPDATE OutboxMessages SET ProcessedAt = @Now, Error = NULL WHERE Id IN @Ids
        """;

    private const string MarkErrorSql = """
        UPDATE OutboxMessages SET Error = @Error WHERE Id IN @Ids
        """;

    private readonly string _connectionString;
    private readonly OutboxOptions _options;
    private readonly RabbitMqPublisher _publisher;
    private readonly ILogger<OutboxPublisherService> _logger;

    public OutboxPublisherService(
        IConfiguration configuration,
        IOptions<OutboxOptions> options,
        RabbitMqPublisher publisher,
        ILogger<OutboxPublisherService> logger)
    {
        _connectionString = configuration.GetConnectionString("WalletDb")
            ?? throw new InvalidOperationException("Falta ConnectionStrings:WalletDb");
        _options = options.Value;
        _publisher = publisher;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox publisher iniciado (lote {Batch}, intervalo {Interval} ms)",
            _options.BatchSize, _options.PollIntervalMs);

        while (!stoppingToken.IsCancellationRequested)
        {
            var processed = 0;
            try
            {
                processed = await ProcessBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falló el ciclo del Outbox; se reintenta en la próxima vuelta");
            }

            // Si había trabajo, vuelve enseguida; si no, espera.
            if (processed == 0)
                await Task.Delay(_options.PollIntervalMs, stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task<int> ProcessBatchAsync(CancellationToken ct)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(ct);
        await using var tx = (SqlTransaction)await conn.BeginTransactionAsync(ct);

        var rows = (await conn.QueryAsync<OutboxRow>(
            new CommandDefinition(SelectSql, new { Batch = _options.BatchSize }, tx, cancellationToken: ct)))
            .ToList();

        if (rows.Count == 0)
        {
            await tx.CommitAsync(ct);
            return 0;
        }

        var ids = rows.Select(r => r.Id).ToArray();

        try
        {
            _publisher.PublishBatch(rows);
        }
        catch (Exception ex)
        {
            // Liberamos los locks y dejamos registro del error. Siguen pendientes.
            await tx.RollbackAsync(ct);

            var error = ex.Message.Length > 500 ? ex.Message[..500] : ex.Message;
            await conn.ExecuteAsync(new CommandDefinition(MarkErrorSql, new { Error = error, Ids = ids }, cancellationToken: ct));

            _logger.LogWarning(ex, "No se pudo publicar un lote de {Count} mensajes", rows.Count);
            return 0;
        }

        await conn.ExecuteAsync(new CommandDefinition(
            MarkProcessedSql, new { Now = DateTime.UtcNow, Ids = ids }, tx, cancellationToken: ct));
        await tx.CommitAsync(ct);

        _logger.LogInformation("Publicados {Count} mensajes del Outbox", rows.Count);
        return rows.Count;
    }
}