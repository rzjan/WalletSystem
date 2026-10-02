using NSubstitute;
using WalletSystem.Application.Bets.Commands.CancelBet;
using WalletSystem.Application.Common.Interfaces;
using WalletSystem.Domain.Bets;
using WalletSystem.Domain.Common;
using WalletSystem.Domain.Wallets;

namespace WalletSystems.UnitTest.Application.Bets;

public class CancelBetCommandHandlerTests
{
    private readonly IBetRepository _betRepository = Substitute.For<IBetRepository>();
    private readonly IWalletRepository _walletRepository = Substitute.For<IWalletRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CancelBetCommandHandler _handler;

    public CancelBetCommandHandlerTests(CancelBetCommandHandler handler)
    {
        _handler = handler;
    }

    // Wallet con 200, apuesta de 100 ya debitada: saldo 100 
    private (Wallet wallet, Bet bet) PlacedBetWithDebitedWallet()
    {
        var wallet = Wallet.Create(Guid.NewGuid(), "ARS").Value;
        var seed = wallet.RequestDeposit(Money.Create(200, "ARS").Value, "seed").Value;
        wallet.CompletedDeposit(seed.Id);
        var stake = Money.Create(100, "ARS").Value;
        wallet.DebitForBet(stake, "bet-key");
        var bet = Bet.Place(wallet.Id, stake, Odds.Create(2.5m).Value, "bet-key").Value;
        _betRepository.GetByIdAsync(bet.Id, Arg.Any<CancellationToken>()).Returns(bet);
        _walletRepository.GetByIdForIdempotentOperationAsync(wallet.Id, Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(wallet);
        return (wallet, bet);
    }
    [Fact]
    public async Task Handle_WhenPlaced_CancelsAndRefundsStakeToWallet()
    {
        var (wallet, bet) = PlacedBetWithDebitedWallet();

        var result = await _handler.Handle(new CancelBetCommand(bet.Id), CancellationToken.None);

        Assert.Equal("Cancelled", result.Status);
        Assert.Equal(100, result.RefundedAmount);
        Assert.Equal(200, wallet.Balance.Amount); // 100 + reembolso de 100
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCancelledTwice_RefundsOnlyOnce()
    {
        var (wallet, bet) = PlacedBetWithDebitedWallet();
        var command = new CancelBetCommand(bet.Id);

        await _handler.Handle(command, CancellationToken.None);
        await _handler.Handle(command, CancellationToken.None); // reproceso del mismo mensaje

        Assert.Equal(200, wallet.Balance.Amount); // no 300
        Assert.Single(wallet.Transactions, t => t.Type == TransactionType.BetRefunded);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenAlreadyWon_ThrowsConflictAndDoesNotTouchWallet()
    {
        var (wallet, bet) = PlacedBetWithDebitedWallet();
        bet.MarkAsWon(); // la apuesta ya se resolvió antes de intentar cancelarla

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(new CancelBetCommand(bet.Id), CancellationToken.None));

        Assert.Equal(100, wallet.Balance.Amount); // sin cambios
        await _walletRepository.DidNotReceive().GetByIdForIdempotentOperationAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
