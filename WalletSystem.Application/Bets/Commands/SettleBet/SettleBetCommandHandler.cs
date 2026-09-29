using MediatR;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WalletSystem.Application.Common.Interfaces;
using WalletSystem.Domain.Bets;

namespace WalletSystem.Application.Bets.Commands.SettleBet;

public class SettleBetCommandHandler : IRequestHandler<SettleBetCommand, SettleBetResult>
{
    private readonly IBetRepository _betRepository;
    private readonly IWalletRepository _walletRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SettleBetCommandHandler(IBetRepository betRepository, IWalletRepository walletRepository, IUnitOfWork unitOfWork)
    {
        _betRepository = betRepository;
        _walletRepository = walletRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<SettleBetResult> Handle(SettleBetCommand request, CancellationToken cancellationToken)
    {
        var bet = await _betRepository.GetByIdAsync(request.BetId, cancellationToken)
            ?? throw new KeyNotFoundException($"Bet {request.BetId} no encontrada.");

        var target = request.OutCome == BetOutcome.Won ? BetStatus.Won : BetStatus.Lost;

        //Idempotencia por estado: ya resuelta con el mismo resultado, no hay nada que hacer
        if (bet.Status == target)
            return ToResult(bet);

        // Resultado contradictorio (ya perdida y ahora llega "ganada"): es un conflicto real
        if (bet.Status != BetStatus.Placed)
            throw new InvalidOperationException($"La apuesta ya fue resuelta como {bet.Status}; no puede resolverse como {target}");

        var settleResult = request.OutCome == BetOutcome.Won ? bet.MarkAsWon() : bet.MarkAsLost();
        if(!settleResult.IsSuccess)
            throw new InvalidOperationException(settleResult.Error);

        if(request.OutCome == BetOutcome.Won)
        {
            var wallet = await _walletRepository.GetByIdAsync(bet.WalletID, cancellationToken)
                ?? throw new KeyNotFoundException($"Wallet {bet.WalletID} no encontrada.");

            var creditResult = wallet.CreditPrize(bet.Payout!, $"bet-{bet.Id}-prize");
            if (!creditResult.IsSuccess)
                throw new InvalidOperationException(creditResult.Error);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToResult(bet);
    }

    private static SettleBetResult ToResult(Bet bet) =>
        new(bet.Id, bet.Status.ToString(), bet.Payout?.Amount ?? 0, bet.Stake.Currency);
}
