using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace WalletSystem.Infrastructure.Persistence;

// Solo la usa la herramienta dotnet-ef al generar y aplicar migraciones.
// No participa cuando la aplicación corre.
public class WalletDbContextFactory : IDesignTimeDbContextFactory<WalletDbContext>
{
    public WalletDbContext CreateDbContext(string[] args)
    {

        var connectionString = Environment.GetEnvironmentVariable("WALLET_DB_CONNECTION")
            ?? throw new InvalidOperationException(
                "Definí la variable de entorno WALLET_DB_CONNECTION antes de usar dotnet ef.");

        var options = new DbContextOptionsBuilder<WalletDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new WalletDbContext(options);
    }
}
