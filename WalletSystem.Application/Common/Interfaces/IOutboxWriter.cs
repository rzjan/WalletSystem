namespace WalletSystem.Application.Common.Interfaces;

public interface IOutboxWriter
{
    Task WriteAsync(string eventType, object payload, CancellationToken cancellation);
}
