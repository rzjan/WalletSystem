using MediatR;

namespace WalletSystem.Application.Wallets.Commands.CreateWallet;

public record CreateWalletCommand(
    Guid UserId, String Currency
    ):IRequest<CreateWalletResult>;

public record CreateWalletResult(
        Guid WalletId,
        Guid UserId,
        decimal Balance,
        string Currency);
