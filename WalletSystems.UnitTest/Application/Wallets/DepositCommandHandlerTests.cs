using NSubstitute;
using WalletSystem.Application.Common.Interfaces;
using WalletSystem.Application.Wallets.Commands.Deposit;
using WalletSystem.Domain.Common;
using WalletSystem.Domain.Wallets;

namespace WalletSystems.UnitTest.Application.Wallets;

public class DepositCommandHandlerTests
{
    private readonly IWalletRepository _walletRepository = Substitute.For<IWalletRepository>();
    private readonly IPaymentProviderClient _paymentProviderClient = Substitute.For<IPaymentProviderClient>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly DepositCommandHandler _handler;

    public DepositCommandHandlerTests(DepositCommandHandler handler)
    {
        _handler = handler;
    }

    private Wallet WalletFor()
    {
        var wallet = Wallet.Create(Guid.NewGuid(), "ARS");
        _walletRepository.GetByIdAsync(wallet.Value.Id, Arg.Any<CancellationToken>()).Returns(wallet.Value);
        return wallet.Value;
    }

    [Fact]
    public async Task Handle_WhenProviderApproves_CompletesDepositAndCreditsBalance()
    {
        var wallet = WalletFor();
        _paymentProviderClient.ChargeAsync(100, "ARS", "key-1", Arg.Any<CancellationToken>())
            .Returns(new PaymentProviderResult(true, "ext-123", null));

        var result = await _handler.Handle(
                new DepositCommand(wallet.Id, 100, "ARS", "key-1"), CancellationToken.None);
        Assert.Equal("Completed", result.Status);
        Assert.Equal(100, result.NewBalance);
        await _unitOfWork.Received(2).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
    [Fact]
    public async Task Handle_WhenProviderRejects_FailsDepositWithoutCreditingBalance()
    {
        var wallet = WalletFor();
        _paymentProviderClient.ChargeAsync(100, "ARS", "key-2", Arg.Any<CancellationToken>())
            .Returns(new PaymentProviderResult(false, null, "Tarjeta rechazada"));

        var result = await _handler.Handle(
            new DepositCommand(wallet.Id, 100, "ARS", "key-2"), CancellationToken.None);

        Assert.Equal("Failed", result.Status);
        Assert.Equal(0, result.NewBalance);
    }

    [Fact]
    public async Task Handle_WhenRetriedAfterAlreadyCompleted_DoesNotCallProviderAgain()
    {
        var wallet = WalletFor();
        var transaction = wallet.RequestDeposit(Money.Create(100, "ARS").Value, "key-3").Value;
        wallet.CompletedDeposit(transaction.Id); // simula que un intento anterior ya se resolvió

        var result = await _handler.Handle(
            new DepositCommand(wallet.Id, 100, "ARS", "key-3"), CancellationToken.None);

        Assert.Equal("Completed", result.Status);
        await _paymentProviderClient.DidNotReceive()
            .ChargeAsync(Arg.Any<decimal>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

}
