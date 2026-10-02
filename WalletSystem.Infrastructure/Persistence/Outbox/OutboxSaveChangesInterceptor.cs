using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using WalletSystem.Domain.Common;

namespace WalletSystem.Infrastructure.Persistence.Outbox;

public class OutboxSaveChangesInterceptor: SaveChangesInterceptor
{
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
                DbContextEventData eventData, 
                InterceptionResult<int> result, 
                CancellationToken cancellationToken = default)
    {
        if(eventData.Context is not DbContext context)
                return await base.SavingChangesAsync(eventData, result, cancellationToken);

        var entitiesWithEvents = context.ChangeTracker
            .Entries<Entity>()
            .Select(e => e.Entity)
            .Where(e => e.DomainEvents.Count > 0)
            .ToList();

        foreach (var entity in entitiesWithEvents)
        {
            foreach (var domainEvent in entity.DomainEvents)
            {
                var outboxMessage = new OutboxMessage
                {
                    Id = Guid.NewGuid(),
                    Type = domainEvent.GetType().FullName!,
                    Payload = System.Text.Json.JsonSerializer.Serialize(domainEvent),
                    OccurredAt = DateTime.UtcNow
                };
                context.Set<OutboxMessage>().Add(outboxMessage);
            }
            entity.ClearDomainEvents();
        }
        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
