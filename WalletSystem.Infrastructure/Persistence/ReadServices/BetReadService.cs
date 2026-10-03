using Dapper;
using WalletSystem.Application.Bets.Queries.GetBetById;
using WalletSystem.Application.Common.Interfaces;
using WalletSystem.Domain.Bets;

namespace WalletSystem.Infrastructure.Persistence.ReadServices
{
   public class BetReadService:IBetReadService
    {
       private readonly ISqlConnectionFactory _connectionFactory;
        public BetReadService(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<GetBetByIdQueryResult?> GetByIdAsync(Guid betId, CancellationToken cancellationToken)
        {
            const string sql = """
                SELECT Id as BetId, WalletID, Stake, Odds, Status, Payout, PLaceAt, SettledAt
                FROM Bets
                WHERE Id = @BetId
                """;

            using var conection = _connectionFactory.CreateConnection();
            var command = new CommandDefinition(
                sql, new { BetId = betId }, cancellationToken: cancellationToken);

            var row = await conection.QuerySingleOrDefaultAsync<BetRow>(command);

            if (row is null)            
                return null;

            return new GetBetByIdQueryResult(
                row.Id, row.WalletID, row.Stake, row.Odds, 
                ((BetStatus)row.Status).ToString(), row.Payout, row.PLaceAt, row.SettledAt
                );
        }

        private sealed record BetRow(
            Guid Id,
            Guid WalletID,
            decimal Stake,
            decimal Odds,
            int Status,
            decimal? Payout,
            DateTime PLaceAt,
            DateTime? SettledAt
        );
    }
}
