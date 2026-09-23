using System.Threading.Tasks.Dataflow;
using WalletSystem.Domain.Bets.Events;
using WalletSystem.Domain.Common;

namespace WalletSystem.Domain.Bets;

public class Bet: Entity
{
    public Guid Id { get; set; }
    public Guid WalletID { get; private set; }
    //Apostar - El valor
    public Money Stake { get; private set; }
    // Cuotas de apuestas
    public Odds Odds { get; private set; }
    public BetStatus Status { get; private set; }
    // Pago
    public Money? Payout { get; private set; }
    public string IdempotencyKey { get; private set; }
    public DateTime PlacedAt { get; private set; }
    //Fecha Liquidado o de operación
    public DateTime? SettledAt { get; private set; }

    private Bet() { } //Para EF

    public Bet( Guid walletId, Money stake, Odds odds, 
                string idempoTencyKey)
    {
        Id= Guid.NewGuid();
        WalletID = walletId;
        Stake = stake;
        Odds = odds;
        Status = BetStatus.Placed;        
        IdempotencyKey = idempoTencyKey;
        PlacedAt = DateTime.UtcNow;        
    }

    public static Result<Bet> Place(
            Guid walletId,
            Money stake,
            Odds odds,
            string idempotencyKey) 
    {
        if (walletId == Guid.Empty)
            return Result.Failure<Bet>("El walletId es requerido.");

        if (stake.Amount <= 0)
            return Result.Failure<Bet>("El monto apostado debe ser mayor a cero.");

        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return Result.Failure<Bet>("La clave de idempotencia es requerida");

        var bet = new Bet(walletId, stake, odds, idempotencyKey);
        bet.RaiseDomainEvent(new BetPlacedEvent(
                                bet.Id, walletId, 
                                stake.Amount, stake.Currency, 
                                odds.Value));

        return Result.Success(bet);
    }

    //Marcar como ganado
    public Result MarkAsWon() 
    {
        if (Status != BetStatus.Placed)
            return Result.Failure($"No se puede resolver una apuesta en etado Placed. Estado actual: {Status}.");

        Status = BetStatus.Won;
        Payout = Odds.CaculatePayout(Stake);
        SettledAt = DateTime.UtcNow;

        RaiseDomainEvent(new BetSettledEvent(
                    Id, WalletID, Status, 
                    Payout.Amount, Payout.Currency));

        return Result.Success();
    }

    //Marcar como perdido
    public Result MarkAsLost() 
    { 
        if(Status != BetStatus.Placed)
            return Result.Failure($"Solo se puede resolver una apuesta en estado Placed, Estado actual: {Status}.");
    
        Status = BetStatus.Lost;
        Payout = Money.Zero(Stake.Currency); //Pago
        SettledAt = DateTime.UtcNow;

        RaiseDomainEvent(new BetSettledEvent(
                    Id, WalletID, Status,
                    Payout.Amount, Payout.Currency));

        return Result.Success();
    }


    //Cancelar
    public Result Cancel()
    {
        //Placed => Apuesta realizada
        if (Status != BetStatus.Placed)
            return Result.Failure($"Solo se puede cancelar una apuesta no resuelta. Estado actual: {Status}.");

        Status = BetStatus.Cancelled;
        SettledAt = DateTime.UtcNow; //Fecha Liquidado o de operación

        RaiseDomainEvent(new BetCancelledEvent(Id, WalletID));

        return Result.Success();
    }
}
