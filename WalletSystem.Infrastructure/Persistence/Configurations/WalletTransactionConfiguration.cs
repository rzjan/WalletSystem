using global::WalletSystem.Domain.Wallets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WalletSystem.Infrastructure.Persistence.Configurations;

public class WalletTransactionConfiguration : IEntityTypeConfiguration<WalletTransaction>
{
    public void Configure(EntityTypeBuilder<WalletTransaction> builder)
    {
        builder.ToTable("WalletTransactions");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();

        // Conversión explícita a int: el valor numérico es parte del contrato de la tabla
        builder.Property(t => t.Type).HasConversion<int>().IsRequired();
        builder.Property(t => t.Status).HasConversion<int>().IsRequired();

        builder.Property(t => t.IdempotencyKey).HasMaxLength(100).IsRequired();
        builder.Property(t => t.FailureReason).HasMaxLength(200);
        builder.Property(t => t.CreatedAt).IsRequired();

        builder.OwnsOne(t => t.Amount, money =>
        {
            money.Property(m => m.Amount).HasColumnName("Amount").HasPrecision(18, 2).IsRequired();
            money.Property(m => m.Currency).HasColumnName("Currency")
                .HasMaxLength(3).IsFixedLength().IsUnicode(false).IsRequired();
        });
        builder.Navigation(t => t.Amount).IsRequired();

        // Última línea de defensa de la idempotencia, a nivel base de datos
        builder.HasIndex(t => new { t.WalletId, t.IdempotencyKey }).IsUnique();

        // Sirve al listado paginado (ORDER BY CreatedAt DESC) que va a hacer Dapper
        builder.HasIndex(t => new { t.WalletId, t.CreatedAt }).IsDescending(false, true);
    }
}
