using MediatR;
using System.Security.AccessControl;
using WalletSystem.Application.Common.Interfaces;
using WalletSystem.Domain.Common;

namespace WalletSystem.Application.Wallets.Commands.Deposit;

public class DepositCommandHandler : IRequestHandler<DepositCommand, DepositResult>
{
    private readonly IWalletRepository _walletRepository;
    private readonly IOutboxWriter _outboxWriter;
    private readonly IUnitOfWork _unitOfWork;

    public DepositCommandHandler(
            IWalletRepository walletRepository, 
            IOutboxWriter outboxWriter, 
            IUnitOfWork unitOfWork)
    {
        _walletRepository = walletRepository;
        _outboxWriter = outboxWriter;
        _unitOfWork = unitOfWork;
    }

    public async Task<DepositResult> Handle(
            DepositCommand request, 
            CancellationToken cancellationToken)
    {
        //Obtiene la billetera (Wallet) existente
        var wallet = await _walletRepository.GetByIdAsync(request.WalletID, cancellationToken)
            ?? throw new KeyNotFoundException($"Wallet {request.WalletID} no encontrada.");

        //Construir el value object la moneda
        var amountResult = Money.Create(request.Amount, request.Currency);
        if (!amountResult.IsSuccess)
            throw new InvalidOperationException(amountResult.Error);

        //Se genera el deposito y lo maneja el Aggregate wallet
        var depositResult = wallet.Deposit(amountResult.Value, request.IdempotencyKey);
        if (!depositResult.IsSuccess)
            throw new InvalidOperationException(depositResult.Error);


        var transaction = depositResult.Value;

        await _outboxWriter.WriteAsync(
            eventType: "WalletTransactionCompleted",
            payload: new 
            {
                TransactionId = transaction.Id,
                WalletId = wallet.Id,
                Amount = transaction.Amount,
                Currency = transaction.Amount.Currency,
                Type = transaction.Type.ToString(),
                OcurredAt = transaction.CompletedAt
            },
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new DepositResult(transaction.Id, wallet.Balance.Amount, transaction.Status.ToString());
    }
}
