using MediatR;

namespace WalletSystem.Application.Wallets.Queries.GetWalletTransactions;

public record GetWalletTransactionsQuery
(
    Guid WalletId) : IRequest<List<WalletTransactionDto>>;

public record WalletTransactionDto(
        Guid Id,
        decimal Amount,
        string Currency,
        string Type,
        string Status,
        DateTime CreatedAt,
        DateTime? CompletedAt
    );

