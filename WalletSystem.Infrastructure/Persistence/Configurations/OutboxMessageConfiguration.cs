using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WalletSystem.Infrastructure.Persistence.Outbox;

namespace WalletSystem.Infrastructure.Persistence.Configurations;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Type).HasMaxLength(200).IsRequired();
        builder.Property(m => m.Payload).IsRequired(); // nvarchar(max) por defecto
        builder.Property(m => m.OccurredAt).IsRequired();
        builder.Property(m => m.Error).HasMaxLength(1000);

        // El publisher (Worker) va a hacer: WHERE ProcessedAt IS NULL ORDER BY OccurredAt
        builder.HasIndex(m => new { m.ProcessedAt, m.OccurredAt });
    }
}
