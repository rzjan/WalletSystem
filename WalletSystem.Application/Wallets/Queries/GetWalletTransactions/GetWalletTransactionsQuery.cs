using MediatR;
using WalletSystem.Application.Common.Models;

namespace WalletSystem.Application.Wallets.Queries.GetWalletTransactions;

public record GetWalletTransactionsQuery
(
    Guid WalletId,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<WalletTransactionDto>>;

public record WalletTransactionDto(
        Guid Id,
        decimal Amount,
        string Currency,
        string Type,
        string Status,
        DateTime CreatedAt,
        DateTime? CompletedAt
    );

