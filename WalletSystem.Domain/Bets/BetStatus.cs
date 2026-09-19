namespace WalletSystem.Domain.Bets;

public enum BetStatus
{
    Placed = 1, //Apuesta realizada
    Won = 2, // Apuesta ganada
    Lost = 3, //Apuesta perdida
    Cancelled = 4 //Apuesta cancelada
}
