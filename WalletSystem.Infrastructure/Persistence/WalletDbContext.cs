using Microsoft.EntityFrameworkCore;
using WalletSystem.Application.Common.Interfaces;
using WalletSystem.Domain.Bets;
using WalletSystem.Domain.Wallets;
using WalletSystem.Infrastructure.Persistence.Outbox;

namespace WalletSystem.Infrastructure.Persistence;

public class WalletDbContext : DbContext, IUnitOfWork
{
    public WalletDbContext(DbContextOptions<WalletDbContext> options) : base(options)
    {
    }

    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<Bet> Bets => Set<Bet>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WalletDbContext).Assembly);
    }
}