using MediatR;
using WalletSystem.Application.Common.Interfaces;
using WalletSystem.Domain.Wallets;

namespace WalletSystem.Application.Wallets.Commands.CreateWallet;

public class CreateWalletComandHandler:IRequestHandler<CreateWalletCommand, CreateWalletResult>
{
    private readonly IWalletRepository _walletRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateWalletComandHandler(IWalletRepository walletRepository, IUnitOfWork unitOfWork)
    {
        _walletRepository = walletRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CreateWalletResult> Handle(CreateWalletCommand request, CancellationToken cancellationToken)
    {
        var existing = await _walletRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (existing is not null)
        {
            if(existing.Balance.Currency != request.Currency.ToUpperInvariant())
                                throw new InvalidOperationException("El usuario ya tiene una wallet en otra moneda.");

            return ToResult(existing);
        }

        var walletResult = Wallet.Create(request.UserId, request.Currency);
        if(!walletResult.IsSuccess)
            throw new InvalidOperationException(walletResult.Error);

        var wallet = walletResult.Value;
        _walletRepository.Add(wallet);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToResult(wallet);
    }

    private static CreateWalletResult ToResult(Wallet wallet) =>
        new(wallet.Id, wallet.UserId, wallet.Balance.Amount, wallet.Balance.Currency);
}
