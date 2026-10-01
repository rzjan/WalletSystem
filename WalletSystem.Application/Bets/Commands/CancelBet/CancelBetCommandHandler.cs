using MediatR;
using WalletSystem.Application.Common.Interfaces;
using WalletSystem.Domain.Bets;

namespace WalletSystem.Application.Bets.Commands.CancelBet;

public class CancelBetCommandHandler : IRequestHandler<CancelBetCommand, CancelBetResult>
{
    private readonly IBetRepository _betRepository;
    private readonly IWalletRepository _walletRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CancelBetCommandHandler(
        IBetRepository betRepository,
        IWalletRepository walletRepository,
        IUnitOfWork unitOfWork)
    {
        _betRepository = betRepository;
        _walletRepository = walletRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CancelBetResult> Handle(CancelBetCommand request, CancellationToken cancellationToken)
    {
        var bet = await _betRepository.GetByIdAsync(request.BetId, cancellationToken)
            ?? throw new KeyNotFoundException($"Bet {request.BetId} no encontrada.");

        if (bet.Status == BetStatus.Cancelled)
            return ToResult(bet); // idempotente: ya cancelada y reembolsada

        var cancelResult = bet.Cancel();
        if (!cancelResult.IsSuccess)
            throw new InvalidOperationException(cancelResult.Error);

        var wallet = await _walletRepository.GetByIdForIdempotentOperationAsync(
                    bet.WalletId,$"bet-{bet.Id}-refund", cancellationToken)
            ?? throw new KeyNotFoundException($"Wallet {bet.WalletId} no encontrada.");

        var refundResult = wallet.RefundBet(bet.Stake, $"bet-{bet.Id}-refund");
        if (!refundResult.IsSuccess)
            throw new InvalidOperationException(refundResult.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToResult(bet);
    }

    private static CancelBetResult ToResult(Bet bet) =>
        new(bet.Id, bet.Status.ToString(), bet.Stake.Amount, bet.Stake.Currency);
}
