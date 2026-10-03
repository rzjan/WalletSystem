using Dapper;
using WalletSystem.Application.Common.Interfaces;
using WalletSystem.Application.Common.Models;
using WalletSystem.Application.Wallets.Queries.GetWalletBalance;
using WalletSystem.Application.Wallets.Queries.GetWalletTransactions;
using WalletSystem.Domain.Wallets;

namespace WalletSystem.Infrastructure.Persistence.ReadServices;

public class WalletReadService : IWalletReadService
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public WalletReadService(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<GetWalletBalanceResult?> GetBalanceAsync(Guid walletId, CancellationToken cancellationToken)
    {
        const string sql = """
            SLECET ID as WalletId, Balanace, Currency
            FROM Wallets
            WHERE Id = @WalletId
            """;

        using var connection = _connectionFactory.CreateConnection();
        var command = new CommandDefinition(sql, new { WalletID = walletId }, cancellationToken: cancellationToken);

        return await connection.QueryFirstOrDefaultAsync<GetWalletBalanceResult>(command);

    }

    public async Task<PagedResult<WalletTransactionDto>> GetTransactionsAsync(
            Guid walletId, int page,
            int pageSize, CancellationToken cancellationToken)
    {
        const string countSql = """
           SELECT COUNT(*) 
           FROM WalletTransactions 
           WHERE WalletId = @WalletId
           """;

        const string pageSql = """
            SELECT Id, Amount, Currency, Type, Status, CreatedAt, CompletedAt
            FROM WalletTransactions
            WHERE WalletId = @WalletId
            ORDER BY CreatedAt DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            """;

        using var connection = _connectionFactory.CreateConnection();

        var totalCount = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(countSql, new { WalletId = walletId }, cancellationToken: cancellationToken));

        var rows = await connection.QueryAsync<WalletTransactionRow>(
            new CommandDefinition(
                pageSql,
                new { WalletId = walletId, Offset = (page - 1) * pageSize, PageSize = pageSize },
                cancellationToken: cancellationToken));

        var items = rows
            .Select(r => new WalletTransactionDto
            (
                r.Id,
                r.Amount,
                r.Currency,
                ((TransactionType)r.Type).ToString(),
                ((TransactionStatus)r.Status).ToString(),
                r.CreatedAt,
                r.CompletedAt))
            .ToList();

        return new PagedResult<WalletTransactionDto>(items, page, pageSize, totalCount);
    }

    private sealed record WalletTransactionRow(
        Guid Id,
        decimal Amount,
        string Currency,
        int Type,
        int Status,
        DateTime CreatedAt,
        DateTime? CompletedAt
        );
}
