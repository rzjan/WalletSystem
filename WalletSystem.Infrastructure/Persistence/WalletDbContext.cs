using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using WalletSystem.Application.Common.Interfaces;
using WalletSystem.Domain.Bets;
using WalletSystem.Domain.Wallets;

namespace WalletSystem.Infrastructure.Persistence;

public class WalletDbContext : DbContext, IUnitOfWork
{
    public WalletDbContext(DbContextOptions<WalletDbContext> options): base(options)
    {       
    }

    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<Bet> Bets => Set<Bet>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {        
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WalletDbContext).Assembly);
    }
}