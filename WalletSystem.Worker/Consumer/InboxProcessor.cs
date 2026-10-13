using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace WalletSystem.Worker.Consumer;

public enum ProcessResult { Processed, Duplicate }

public sealed class InboxProcessor
{
    private const string InsertSql = """
        INSERT INTO InboxMessages (ConsumerName, MessageId, ProcessedAt)
        VALUES (@Consumer, @MessageId, @Now)
        """;

    private const int PrimaryKeyViolation = 2627;
    private const int UniqueIndexViolation = 2601;

    private readonly string _connectionString;
    private readonly string _consumerName;
    private readonly FaultInjector _faults;
    private readonly IEnumerable<IEventHandler> _handlers;

    public InboxProcessor(
        IConfiguration configuration,
        IOptions<ConsumerOptions> options,
        FaultInjector faults,
        IEnumerable<IEventHandler> handlers)
    {
        _connectionString = configuration.GetConnectionString("WalletDb")
            ?? throw new InvalidOperationException("Falta ConnectionStrings:WalletDb");
        _consumerName = options.Value.QueueName;
        _faults = faults;
        _handlers = handlers;
    }

    public async Task<ProcessResult> ProcessAsync(EventContext context, CancellationToken ct)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(ct);
        await using var tx = (SqlTransaction)await conn.BeginTransactionAsync(ct);

        try
        {
            await conn.ExecuteAsync(new CommandDefinition(
                InsertSql,
                new { Consumer = _consumerName, context.MessageId, Now = DateTime.UtcNow },
                tx, cancellationToken: ct));
        }
        catch (SqlException ex) when (ex.Number is PrimaryKeyViolation or UniqueIndexViolation)
        {
            // Ya lo procesó este consumer (o lo está procesando otra instancia).
            await tx.RollbackAsync(ct);
            return ProcessResult.Duplicate;
        }

        await _faults.ApplyAsync(context, ct);

        foreach (var handler in _handlers)
            await handler.HandleAsync(context, conn, tx, ct);

        // Si algo de arriba lanzó, la transacción se revierte al salir (Dispose) y el INSERT
        // del Inbox desaparece: el reintento vuelve a procesar el mensaje completo.
        await tx.CommitAsync(ct);
        return ProcessResult.Processed;
    }
}
