using MediatR;
using System.Reflection.Metadata;
using WalletSystem.Application.Common.Interfaces;
using WalletSystem.Domain.Bets;
using WalletSystem.Domain.Common;

namespace WalletSystem.Application.Bets.Commands.PlaceBet;

public class PlaceBetCommandHandler:IRequestHandler<PlaceBetCommand, PlaceBetResult>
{
    private readonly IWalletRepository _walletRepository;
    private readonly IBetRepository _betRepository;
    private readonly IUnitOfWork _unitOfWork;

    public PlaceBetCommandHandler(IWalletRepository walletRepository, IBetRepository betRepository, IUnitOfWork unitOfWork)
    {
        _walletRepository = walletRepository;
        _betRepository = betRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<PlaceBetResult> Handle(PlaceBetCommand request, CancellationToken cancellationToken)
    {
        // 0) Idempotencia a nivel de caso de uso: Si esta apuesta ua se colocó, devolvemos el mismo resultado.                
        var existingBet = await _betRepository.GetByIdempotencyKeyAsync(
            request.WalletId, request.IdempotencyKey, cancellationToken);
        if (existingBet is not null)
        {
            var existingWallet = await _walletRepository.GetByIdAsync(existingBet.WalletId, cancellationToken);
            return new PlaceBetResult(existingBet.Id, existingWallet!.Balance.Amount, existingBet.Status.ToString());
        }
        // 1) Cargar el primer aggregate
        var wallet = await _walletRepository.GetByIdAsync(request.WalletId, cancellationToken)
            ?? throw new KeyNotFoundException($"Wallet {request.WalletId} no encontrada.");

        //2) Construir los value objects - Si algo es inválido, cortamos ANTES de tocar el salgo.
        var stakeResult = Money.Create(request.Stake, request.Currency);
        if (!stakeResult.IsSuccess)
            throw new InvalidOperationException(stakeResult.Error);
        
        var oddsResult = Odds.Create(request.Odds);
        if (!oddsResult.IsSuccess)
            throw new InvalidOperationException(oddsResult.Error);

        // 3) Debitar la wallet - si no hay fondos, el flujo se corta ACÁ, la Bet nunca se crea.
        var debitResult = wallet.DebitForBet(stakeResult.Value, request.IdempotencyKey);
        if (!debitResult.IsSuccess)
            throw new InvalidOperationException(debitResult.Error);

        // 4) Recien ahora, con el débito confirmado, se crea el segundo aggregate
        var betResult = Bet.Place(wallet.Id, stakeResult.Value, oddsResult.Value, request.IdempotencyKey);
        if (!betResult.IsSuccess)            
            throw new InvalidOperationException(betResult.Error);

        var bet = betResult.Value;
        _betRepository.Add(bet);

        // 5) Un solo SaveChangesAsync - Wallet (modificada) y Bet (nueva) se persisten juntas, atomicamente 
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new PlaceBetResult(bet.Id, wallet.Balance.Amount, bet.Status.ToString());
    }
}
