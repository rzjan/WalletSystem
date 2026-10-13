using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WalletSystem.Infrastructure.Persistence.Inbox;

public sealed class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        builder.ToTable("InboxMessages");
        builder.HasKey(x => new { x.ConsumerNamer, x.MessageId });

        builder.Property(x => x.ConsumerNamer)
            .IsRequired()
            .HasMaxLength(200);
        builder.Property(x => x.MessageId).ValueGeneratedNever();

        //Para la limpieza periodica de mensajes viejos.
        builder.HasIndex(x => x.ProcessedAt);
    }
}
