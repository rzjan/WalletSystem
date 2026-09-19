using WalletSystem.Domain.Common;
using WalletSystem.Domain.Wallets;

namespace WalletSystems.UnitTest.Domain;

public class WalletTests
{
    private static Wallet CreateWallet(decimal initialBalance = 0)
    {
        var wallet = Wallet.Create(Guid.NewGuid(), "ARS").Value;

        if (initialBalance > 0)
        {
            var deposit = Money.Create(initialBalance, "ARS").Value;
            wallet.Deposit(deposit, Guid.NewGuid().ToString());
        }
        return wallet;
    }

    [Fact]
    public void Deposit_WithValidAmount_IncreaseBalance() 
    {
        var wallet = CreateWallet();
        var amount = Money.Create(100, "ARS").Value;

        var result = wallet.Deposit(amount, "key-1");

        Assert.True(result.IsSuccess);
        Assert.Equal(100, wallet.Balance.Amount);
    }

    [Fact]
    public void Withdraw_WithInsuficientFunds_ReturnsFailureAndDoesNotChangeBalance() 
    {
        var wallet = CreateWallet(initialBalance: 50);
        var amount = Money.Create(100, "ARS").Value;

        var result = wallet.Withdraw(amount, "key-2");

        Assert.False(result.IsSuccess);
        Assert.Equal("Fondos insuficientes.", result.Error);
        Assert.Equal(50, wallet.Balance.Amount);
    }

    [Fact]
    public void WithDraw_WithInsuficientFunds_RecordsFailedTransaction() 
    {
        var wallet = CreateWallet(initialBalance: 50);
        var amount = Money.Create(100,"ARS").Value;

        wallet.Withdraw(amount, "key-3");

        var transaction = wallet.Transactions.Single(t=>t.IdempotencyKey == "key-3");
        Assert.Equal(TransactionStatus.Failed, transaction.Status);
    }

    [Fact]
    public void Applytransaction_withSameIdempotencyKeyTwice_AppliesOnlyOnce() 
    {
        var wallet = CreateWallet();
        var amount = Money.Create(100,"ARS").Value;

        wallet.Deposit(amount, "key-4");
        wallet.Deposit(amount, "key-4"); //Mismo key se reintenta

        Assert.Equal(100, wallet.Balance.Amount); // no 200
        Assert.Single(wallet.Transactions);
    }

    [Fact]
    public void ApplyTransaction_withSaameIdempotencyKeyTwice_ReturnsSameTransactionOnSecondCall() 
    {
        var wallet = CreateWallet();
        var amount = Money.Create(100, "ARS").Value;

        var first =  wallet.Deposit(amount, "key-5");
        var second = wallet.Deposit(amount, "key-5");

        Assert.Equal(first.Value.Id, second.Value.Id);
    }

    [Fact]
    public void DebitForBet_WithSufficientFunds_DecreasesBalance() 
    {
        var wallet = CreateWallet();
        var stake = Money.Create(50, "ARS").Value;

        var result = wallet.DebitForBet(stake, "key-6");

        Assert.True(result.IsSuccess);
        Assert.Equal(150, wallet.Balance.Amount);
        Assert.Equal(TransactionType.BetPlaced, result.Value.Type);
    }
    
    [Fact]
    public void CreditPrize_AddsToBalance() 
    {
        var wallet = CreateWallet(initialBalance: 100);
        var prize = Money.Create(250, "ARS").Value;

        var result = wallet.CreditPrize(prize, "key-7");

        Assert.True(result.IsSuccess);
        Assert.Equal(350, wallet.Balance.Amount);
    }

}
