using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WalletSystem.Domain.Wallets;

namespace WalletSystem.Infrastructure.Persistence.Configurations;

public class WalletConfiguration : IEntityTypeConfiguration<Wallet>
{
    public void Configure(EntityTypeBuilder<Wallet> builder)
    {
        builder.ToTable("Wallets");

        builder.HasKey(w => w.Id);
        builder.Property(w => w.Id).ValueGeneratedNever();

        builder.Property(w => w.UserId).IsRequired();
        builder.HasIndex(w => w.UserId).IsUnique();
        builder.Property(w => w.CreatedAt).IsRequired();

        builder.OwnsOne(w => w.Balance, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("Balance")
                .HasPrecision(18, 2)
                .IsRequired();

            money.Property(m => m.Currency)
                .HasColumnName("Currency")
                .HasMaxLength(3)
                .IsFixedLength()
                .IsUnicode(false)
                .IsRequired();
        });

        builder.Navigation(w => w.Balance).IsRequired();

        // Token de concurrencia optimista, como shadow property: el Domain no sabe que existe. 
        builder.Property<Byte[]>("RowVersion").IsRowVersion();

        builder.HasMany(w=> w.Transactions)
            .WithOne()
            .HasForeignKey(t=> t.WalletId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(w => w.Transactions)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(w=> w.DomainEvents);
    }
}
