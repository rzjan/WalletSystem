using WalletSystem.Domain.Bets;
using WalletSystem.Domain.Bets.Events;
using WalletSystem.Domain.Common;

namespace WalletSystems.UnitTest.Domain;

public class BetTest
{
    private static Money Stake(decimal amount = 100) => Money.Create(amount, "ARS").Value;
    private static Odds FixedOdds(decimal value = 2.5m) => Odds.Create(value).Value;

    [Fact]
    public void Place_WithValidData_CreatesBetInPlacedStatus() 
    {
        var result = Bet.Place(Guid.NewGuid(), Stake(), FixedOdds(), "bet-key-1");

        Assert.True(result.IsSuccess);
        Assert.Equal(BetStatus.Placed, result.Value.Status);
    }

    [Fact]
    public void Place_WithZeroWalletId_Fails()
    {
        var result = Bet.Place(Guid.Empty, Stake(), FixedOdds(), "bet-key-2");

        Assert.False(result.IsSuccess);
        Assert.Equal("El WalletId es requerido.", result.Error);
    }
    [Fact]
    public void Place_WithZeroStake_Fails()
    {
        var zeroStake = Money.Zero("ARS");

        var result = Bet.Place(Guid.NewGuid(),zeroStake, FixedOdds(), "bet-key-3");

        Assert.False(result.IsSuccess);
        Assert.Equal("El monto apostado debe ser mayor a cero.", result.Error);

    }
    [Fact]
    public void Place_WithoutIdempotencyKey_Fails()
    {
        var result = Bet.Place(Guid.NewGuid(), Stake(), FixedOdds(), "");

        Assert.False(result.IsSuccess);
        Assert.Equal("La clave de idempotencia es requerida.", result.Error);
    }

    [Fact]
    public void MarkAsWon_CalculatesPayoutUsingOdds()
    {
        var bet = Bet.Place(Guid.NewGuid(), Stake(100), FixedOdds(2.5m), "bet-key-4").Value;

        var result = bet.MarkAsWon();

        Assert.True(result.IsSuccess);
        Assert.Equal(BetStatus.Won, bet.Status);
        Assert.Equal(250, bet.Payout!.Amount); // 100 * 2.5
    }

    [Fact]
    public void MarkAsLost_SetsPayoutToZero()
    {
        var bet = Bet.Place(Guid.NewGuid(), Stake(100), FixedOdds(2.5m), "bet-key-5").Value;

        var result = bet.MarkAsLost();

        Assert.True(result.IsSuccess);
        Assert.Equal(BetStatus.Lost, bet.Status);
        Assert.Equal(0, bet.Payout!.Amount);
    }

    [Fact]
    public void Cancel_WhenPlaced_SetsStatusToCancelled()
    {
        var bet = Bet.Place(Guid.NewGuid(), Stake(), FixedOdds(), "bet-key-6").Value;

        var result = bet.Cancel();

        Assert.True(result.IsSuccess);
        Assert.Equal(BetStatus.Cancelled, bet.Status);
    }

    [Fact]
    public void MarkAsWon_WhenAlreadySettled_Fails()
    {
        var bet = Bet.Place(Guid.NewGuid(), Stake(), FixedOdds(), "bet-key-7").Value;
        bet.MarkAsWon(); // primera resolución

        var secondAttempt = bet.MarkAsWon(); // ya no está en Placed

        Assert.False(secondAttempt.IsSuccess);
        Assert.Contains("Placed", secondAttempt.Error);
    }

    [Fact]
    public void Cancel_WhenAlreadyWon_Fails()
    {
        var bet = Bet.Place(Guid.NewGuid(), Stake(), FixedOdds(), "bet-key-8").Value;
        bet.MarkAsWon();

        var cancelResult = bet.Cancel();

        Assert.False(cancelResult.IsSuccess);
    }

    [Fact]
    public void MarkAsWon_SetsSettledAtTimestamp()
    {
        var bet = Bet.Place(Guid.NewGuid(), Stake(), FixedOdds(), "bet-key-9").Value;

        bet.MarkAsWon();

        Assert.NotNull(bet.SettledAt);
    }

    [Fact]
    public void Place_WithValidData_RaisesBetPlacedEvent()
    {
        var bet = Bet.Place(Guid.NewGuid(), Stake(100), FixedOdds(2.5m), "bet-evt-1").Value;

        var domainEvent = Assert.Single(bet.DomainEvents);
        var placedEvent = Assert.IsType<BetPlacedEvent>(domainEvent);
        Assert.Equal(100, placedEvent.Stake);
        Assert.Equal(2.5m, placedEvent.Odds);
    }

    [Fact]
    public void MarkAsWon_RaisesBetSettledEventWithCorrectPayout()
    {
        var bet = Bet.Place(Guid.NewGuid(), Stake(100), FixedOdds(2.5m), "bet-evt-2").Value;
        bet.ClearDomainEvents(); // limpio el de Place para aislar el de MarkAsWon

        bet.MarkAsWon();

        var domainEvent = Assert.Single(bet.DomainEvents);
        var settledEvent = Assert.IsType<BetSettledEvent>(domainEvent);
        Assert.Equal(BetStatus.Won, settledEvent.Result);
        Assert.Equal(250, settledEvent.PayoutAmount);
    }

    [Fact]
    public void MarkAsLost_RaisesBetSettledEventWithZeroPayout()
    {
        var bet = Bet.Place(Guid.NewGuid(), Stake(100), FixedOdds(2.5m), "bet-evt-3").Value;
        bet.ClearDomainEvents();

        bet.MarkAsLost();

        var settledEvent = Assert.IsType<BetSettledEvent>(Assert.Single(bet.DomainEvents));
        Assert.Equal(BetStatus.Lost, settledEvent.Result);
        Assert.Equal(0, settledEvent.PayoutAmount);
    }

    [Fact]
    public void Cancel_RaisesBetCancelledEvent()
    {
        var bet = Bet.Place(Guid.NewGuid(), Stake(), FixedOdds(), "bet-evt-4").Value;
        bet.ClearDomainEvents();

        bet.Cancel();

        Assert.IsType<BetCancelledEvent>(Assert.Single(bet.DomainEvents));
    }
}
