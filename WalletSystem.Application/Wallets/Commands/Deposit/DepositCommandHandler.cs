using MediatR;
using WalletSystem.Application.Common.Interfaces;
using WalletSystem.Domain.Common;
using WalletSystem.Domain.Wallets;

namespace WalletSystem.Application.Wallets.Commands.Deposit;

public class DepositCommandHandler : IRequestHandler<DepositCommand, DepositResult>
{
    private readonly IWalletRepository _walletRepository;
    private readonly IPaymentProviderClient _paymentProviderClient;
    private readonly IUnitOfWork _unitOfWork;

    public DepositCommandHandler(
            IWalletRepository walletRepository,
            IPaymentProviderClient paymentProviderClient,
            IUnitOfWork unitOfWork)
    {
        _walletRepository = walletRepository;
        _paymentProviderClient = paymentProviderClient;
        _unitOfWork = unitOfWork;
    }

    public async Task<DepositResult> Handle(
            DepositCommand request,
            CancellationToken cancellationToken)
    {
        //Obtiene la billetera (Wallet) existente
        var wallet = await _walletRepository.GetByIdForIdempotentOperationAsync
                    (request.WalletID, request.IdempotencyKey ,cancellationToken)
                    ?? throw new KeyNotFoundException($"Wallet {request.WalletID} no encontrada.");

        //Construir el value object la moneda
        var amountResult = Money.Create(request.Amount, request.Currency);
        if (!amountResult.IsSuccess)
            throw new InvalidOperationException(amountResult.Error);

        // Fase 1: Registrar la intención. Idempotente - si ya existe, la devuelve tal cual esté.
        var requestResult = wallet.RequestDeposit(amountResult.Value, request.IdempotencyKey);
        if (!requestResult.IsSuccess)
            throw new InvalidOperationException(requestResult.Error);

        var transaction = requestResult.Value;

        // Ya resuelta en un intento anterior: no se vuelve a llamar al proveedor.
        if (transaction.Status != TransactionStatus.Pending)
            return ToResult(transaction, wallet.Balance.Amount);

        // Commit del Pending ANTES de la llamada externa. Si el proceso muere justo después,
        // un job de reconciliación puede encontrar esta fila y resolverla más tarde.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var chargetResult = await _paymentProviderClient.ChargeAsync(
                                amountResult.Value.Amount, amountResult.Value.Currency,
                                request.IdempotencyKey, cancellationToken);

        var settleResult = chargetResult.IsSuccess
            ? wallet.CompletedDeposit(transaction.Id)
            : wallet.FailDeposit(transaction.Id, chargetResult.ErrorMessage ?? "El proveedor de pagos rechazó la operación.");

        if (!settleResult.IsSuccess)
            throw new InvalidOperationException(settleResult.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToResult(settleResult.Value, wallet.Balance.Amount);        
    }

    private static DepositResult ToResult(WalletTransaction transaction, decimal balance) =>
            new(transaction.Id, balance, transaction.Status.ToString());
}
