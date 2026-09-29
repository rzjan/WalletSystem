using NSubstitute;
using WalletSystem.Application.Bets.Commands.SettleBet;
using WalletSystem.Application.Common.Interfaces;
using WalletSystem.Domain.Bets;
using WalletSystem.Domain.Common;
using WalletSystem.Domain.Wallets;

namespace WalletSystems.UnitTest.Application.Bets;

public class SettleBetCommandHandlerTests
{
    private readonly IBetRepository _betRepository = Substitute.For<IBetRepository>();
    private readonly IWalletRepository _walletRepository = Substitute.For<IWalletRepository>();
    private readonly IUnitOfWork _unitOfWork  = Substitute.For<IUnitOfWork>();
    private readonly SettleBetCommandHandler _handler;

    public SettleBetCommandHandlerTests()
    {
        _handler = new SettleBetCommandHandler(_betRepository, _walletRepository, _unitOfWork);
    }

    //creo la wallet con 200, apuesta de 100 a cuota 2.5 ya debitada: saldo 100, premio potencial 250
    private (Wallet wallet, Bet bet) PlacedBetWithDebitedWallet()
    {
        var wallet = Wallet.Create(Guid.NewGuid(), "ARS").Value;
        wallet.Deposit(Money.Create(200, "ARS").Value, "seed");

        var stake = Money.Create(100, "ARS").Value;
        wallet.DebitForBet(stake, "bet-key");

        var bet = Bet.Place(wallet.Id, stake, Odds.Create(2.5m).Value, "bet-key").Value;

        _betRepository.GetByIdAsync(bet.Id, Arg.Any<CancellationToken>()).Returns(bet);
        _walletRepository.GetByIdAsync(wallet.Id, Arg.Any<CancellationToken>()).Returns(wallet);

        return (wallet, bet);
    }

    [Fact]    
    public async Task Handle_WhenWon_CreditsPrizeToWallet()
    {
        var (wallet, bet) = PlacedBetWithDebitedWallet();

        var result = await _handler.Handle(new SettleBetCommand(bet.Id, BetOutcome.Won), CancellationToken.None);

        Assert.Equal(250, result.PayoutAmount);
        Assert.Equal(350, wallet.Balance.Amount); // 100 + 250
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenSettledTwiceWithSameOutcome_CreditsPrizeOnlyOnce()
    {
        var (wallet, bet) = PlacedBetWithDebitedWallet();
        var command = new SettleBetCommand(bet.Id, BetOutcome.Won);

        await _handler.Handle(command, CancellationToken.None);
        await _handler.Handle(command, CancellationToken.None); // reproceso del mismo mensaje

        Assert.Equal(350, wallet.Balance.Amount);
        Assert.Single(wallet.Transactions, t => t.Type == TransactionType.PrizeCredited);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
    [Fact]
    public async Task Handle_WhenLost_DoesNotTouchWallet()
    {
        var (wallet, bet) = PlacedBetWithDebitedWallet();

        var result = await _handler.Handle(new SettleBetCommand(bet.Id, BetOutcome.Lost), CancellationToken.None);

        Assert.Equal(0, result.PayoutAmount);
        Assert.Equal(100, wallet.Balance.Amount);
        await _walletRepository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenAlreadyLostAndNowWon_ThrowsConflict()
    {
        var (_, bet) = PlacedBetWithDebitedWallet();
        await _handler.Handle(new SettleBetCommand(bet.Id, BetOutcome.Lost), CancellationToken.None);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(new SettleBetCommand(bet.Id, BetOutcome.Won), CancellationToken.None));
    }
}
