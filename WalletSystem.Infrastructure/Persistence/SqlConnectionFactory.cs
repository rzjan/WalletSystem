using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace WalletSystem.Infrastructure.Persistence;


public interface ISqlConnectionFactory 
{
    IDbConnection CreateConnection();
}

/// <summary> Conection factory for SQL Server database connections. 
/// Para usar ado net para lecturas de la DB </summary>
public class SqlConnectionFactory : ISqlConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("WalletDb")
                ?? throw new InvalidOperationException("Falta la conection string 'WalletDb'.");
    }

    public IDbConnection CreateConnection()
    {
       return new SqlConnection(_connectionString);
    }
}
