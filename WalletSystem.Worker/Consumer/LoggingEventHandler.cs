using Microsoft.Data.SqlClient;

namespace WalletSystem.Worker.Consumer;

public sealed class LoggingEventHandler : IEventHandler
{
    private readonly ILogger<LoggingEventHandler> _logger;

    public LoggingEventHandler(ILogger<LoggingEventHandler> logger) => _logger = logger;

    public Task HandleAsync(EventContext context, SqlConnection connection, SqlTransaction transaction, CancellationToken ct)
    {
        var shortType = context.Type[(context.Type.LastIndexOf('.') + 1)..];
        _logger.LogInformation("Evento {Type} ({MessageId}) procesado en el intento {Attempt}",
            shortType, context.MessageId, context.Attempt);
        return Task.CompletedTask;
    }
}
