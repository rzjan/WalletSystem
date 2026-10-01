using NSubstitute;
using WalletSystem.Application.Bets.Commands.PlaceBet;
using WalletSystem.Application.Common.Interfaces;
using WalletSystem.Domain.Bets;
using WalletSystem.Domain.Common;
using WalletSystem.Domain.Wallets;

namespace WalletSystems.UnitTest.Application.Bets;

public class PlaceBetCommandHandlerTest
{
    private readonly IWalletRepository _walletRepository = Substitute.For<IWalletRepository>();
    private readonly IBetRepository _betRepository = Substitute.For<IBetRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly PlaceBetCommandHandler _handler;

    public PlaceBetCommandHandlerTest()
    {
        _handler = new PlaceBetCommandHandler(_walletRepository, _betRepository, _unitOfWork);
    }

    [Fact]
    public async Task Hanlde_WithNoExistingBet_DebitsWalletAndCreatesBet()
    {
        var wallet = Wallet.Create(Guid.NewGuid(), "ARS").Value;        
        var seed = wallet.RequestDeposit(Money.Create(200, "ARS").Value, "seed-deposit").Value;
        wallet.CompletedDeposit(seed.Id);

        var command = new PlaceBetCommand(wallet.Id, 100, "ARS", 2.5m, "bet-key-1");


        _betRepository.GetByIdempotencyKeyAsync(wallet.Id, "bet-key-1", Arg.Any<CancellationToken>())
            .Returns((Bet?)null);

        _walletRepository.GetByIdForIdempotentOperationAsync(wallet.Id, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(wallet);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(100, result.NewWalletBalance); //200 - 100
        _betRepository.Received(1).Add(Arg.Any<Bet>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenBetWithSameIdempotencyKeyAlreadyExists_ReturnsExistingBetWithoutTouchingWallet()
    {
        var wallet = Wallet.Create(Guid.NewGuid(), "ARS").Value;        
        var seed = wallet.RequestDeposit(Money.Create(200, "ARS").Value, "seed-deposit").Value;
        wallet.CompletedDeposit(seed.Id);
        wallet.DebitForBet(Money.Create(100, "ARS").Value, "bet-key-2"); // simula que ya se debitó antes

        var existingBet = Bet.Place(wallet.Id, Money.Create(100, "ARS").Value, Odds.Create(2.5m).Value, "bet-key-2").Value;

        var command = new PlaceBetCommand(wallet.Id, 100, "ARS", 2.5m, "bet-key-2");

        _betRepository.GetByIdempotencyKeyAsync(wallet.Id, "bet-key-2", Arg.Any<CancellationToken>())
            .Returns(existingBet);
        _walletRepository.GetByIdAsync(wallet.Id, Arg.Any<CancellationToken>())
            .Returns(wallet);

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(existingBet.Id, result.BetId);
        Assert.Equal(100, result.NewWalletBalance); // no se debitó una segunda vez

        // Lo más importante del test: NUNCA se llamó DebitForBet de nuevo ni se agregó una Bet nueva
        _betRepository.DidNotReceive().Add(Arg.Any<Bet>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithInsufficientFunds_ThrowsAndDoesNotCreateBet()
    {
        var wallet = Wallet.Create(Guid.NewGuid(), "ARS").Value; // saldo 0

        var command = new PlaceBetCommand(wallet.Id, 100, "ARS", 2.5m, "bet-key-3");

        _betRepository.GetByIdempotencyKeyAsync(wallet.Id, "bet-key-3", Arg.Any<CancellationToken>())
            .Returns((Bet?)null);
        _walletRepository.GetByIdAsync(wallet.Id, Arg.Any<CancellationToken>())
            .Returns(wallet);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, CancellationToken.None));

        _betRepository.DidNotReceive().Add(Arg.Any<Bet>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

}
