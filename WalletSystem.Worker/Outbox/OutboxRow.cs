namespace WalletSystem.Worker.Outbox;

public sealed record OutboxRow(Guid Id, string Type, string Payload, DateTime OccurredAt);
