using global::WalletSystem.Domain.Bets;
using global::WalletSystem.Domain.Wallets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WalletSystem.Infrastructure.Persistence.Configurations;

public class BetConfiguration : IEntityTypeConfiguration<Bet>
{
    public void Configure(EntityTypeBuilder<Bet> builder)
    {
        builder.ToTable("Bets");

        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).ValueGeneratedNever();

        builder.Property(b => b.Status).HasConversion<int>().IsRequired();
        builder.Property(b => b.IdempotencyKey).HasMaxLength(100).IsRequired();
        builder.Property(b => b.PlacedAt).IsRequired();

        // Odds es un Value Object de un solo valor: alcanza con un value converter
        builder.Property(b => b.Odds)
            .HasConversion(odds => odds.Value, value => Odds.Create(value).Value)
            .HasPrecision(9, 4)
            .IsRequired();

        // Dos Money en la misma tabla: nombres de columna explícitos para que no colisionen
        builder.OwnsOne(b => b.Stake, money =>
        {
            money.Property(m => m.Amount).HasColumnName("Stake").HasPrecision(18, 2).IsRequired();
            money.Property(m => m.Currency).HasColumnName("StakeCurrency")
                .HasMaxLength(3).IsFixedLength().IsUnicode(false).IsRequired();
        });
        builder.Navigation(b => b.Stake).IsRequired();

        builder.OwnsOne(b => b.Payout, money =>
        {
            money.Property(m => m.Amount).HasColumnName("Payout").HasPrecision(18, 2);
            money.Property(m => m.Currency).HasColumnName("PayoutCurrency")
                .HasMaxLength(3).IsFixedLength().IsUnicode(false);
        });

        builder.Property<byte[]>("RowVersion").IsRowVersion();

        // Referencia por Id en el dominio, pero la base igual garantiza integridad referencial
        builder.HasOne<Wallet>()
            .WithMany()
            .HasForeignKey(b => b.WalletId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(b => new { b.WalletId, b.IdempotencyKey }).IsUnique();

        builder.Ignore(b => b.DomainEvents);
    }
}
