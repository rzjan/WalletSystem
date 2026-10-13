using Microsoft.Data.SqlClient;

namespace WalletSystem.Worker.Consumer;

public sealed record EventContext(Guid MessageId, string Type, string Payload, int Attempt);

public interface IEventHandler
{
    // Recibe la conexión y la transacción abiertas: cualquier escritura que haga el handler
    // se confirma (o se revierte) junto con el registro en InboxMessages.
    Task HandleAsync(EventContext context, SqlConnection connection, SqlTransaction transaction, CancellationToken ct);
}
