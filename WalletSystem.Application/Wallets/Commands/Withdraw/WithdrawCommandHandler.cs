using MediatR;
using WalletSystem.Application.Common.Interfaces;
using WalletSystem.Domain.Common;

namespace WalletSystem.Application.Wallets.Commands.Withdraw;

public class WithdrawCommandHandler: IRequestHandler<WithdrawCommand, WithDrawResult>
{
    private readonly IWalletRepository _walletRepository;
    private readonly IUnitOfWork _unitOfWork;

    public WithdrawCommandHandler(IWalletRepository walletRepository, IUnitOfWork unitOfWork)
    {
        _walletRepository = walletRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<WithDrawResult> Handle(WithdrawCommand request, CancellationToken cancellationToken)
    {
        var wallet = await _walletRepository.GetByIdAsync(request.WalletID, cancellationToken)
            ?? throw new KeyNotFoundException($"Wallet {request.WalletID} no encontrada.");

        var amountResult = Money.Create(request.Amount, request.Currency);
        if (!amountResult.IsSuccess)
            throw new InvalidOperationException(amountResult.Error);

        var withDrawResult = wallet.Withdraw(amountResult.Value, request.IdempotencyKey);
        if(!withDrawResult.IsSuccess)
            throw new InvalidOperationException(withDrawResult.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var transaction = withDrawResult.Value;
        return new WithDrawResult(transaction.Id, wallet.Balance.Amount, transaction.Status.ToString());

    }
}
