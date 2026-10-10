using System;

using Antigraph.Ledger.Domain.Models.Entities;

namespace Antigraph.Ledger.Domain.Tests;

public class AccountTests
{
    private const string Name = "Test Account";

    private static Money Gbp(long minorUnits) => new(minorUnits, Currency.GBP);

    /// <summary>An account in GBP, credited with the given opening amount.</summary>
    private static Account AnAccount(long openingMinorUnits = 0)
    {
        var account = new Account(Guid.NewGuid(), Name, Currency.GBP);

        if (openingMinorUnits > 0)
        {
            account.Credit(Gbp(openingMinorUnits));
        }

        return account;
    }

    /// <summary>Independent oracle: what ReservedBalance should be, computed from the holds.</summary>
    private static Money SumOfActiveHolds(Account account) =>
        account.Holds
            .Where(h => h.HoldState == HoldState.Active)
            .Select(h => h.Amount)
            .Aggregate(Money.Zero(account.Currency), (running, next) => running + next);

    // ---------------------------------------------------------------- construction

    [Fact]
    public void Constructor_ValidArguments_InitialisesToAnEmptyZeroBalanceAccount()
    {
        var id = Guid.NewGuid();

        var account = new Account(id, Name, Currency.GBP);

        Assert.Equal(id, account.Id);
        Assert.Equal(Name, account.AccountName);
        Assert.Equal(Currency.GBP, account.Currency);
        Assert.Equal(Money.Zero(Currency.GBP), account.ActualBalance);
        Assert.Equal(Money.Zero(Currency.GBP), account.ReservedBalance);
        Assert.Equal(Money.Zero(Currency.GBP), account.AvailableBalance);
        Assert.Empty(account.Holds);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_MissingName_ThrowsArgumentException(string? name)
    {
        Assert.Throws<ArgumentException>(() => new Account(Guid.NewGuid(), name!, Currency.GBP));
    }

    [Fact]
    public void Constructor_EmptyId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new Account(Guid.Empty, Name, Currency.GBP));
    }

    // ---------------------------------------------------------------------- credit

    [Fact]
    public void Credit_PositiveAmount_IncreasesActualAndAvailableBalance()
    {
        var account = AnAccount();

        account.Credit(Gbp(100_00));

        Assert.Equal(Gbp(100_00), account.ActualBalance);
        Assert.Equal(Gbp(100_00), account.AvailableBalance);
        Assert.Equal(Money.Zero(Currency.GBP), account.ReservedBalance);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100_00)]
    public void Credit_NonPositiveAmount_ThrowsArgumentException(long minorUnits)
    {
        var account = AnAccount();

        Assert.Throws<ArgumentException>(() => account.Credit(Gbp(minorUnits)));
    }

    [Fact]
    public void Credit_DifferentCurrency_ThrowsArgumentException()
    {
        var account = AnAccount();

        Assert.Throws<ArgumentException>(() => account.Credit(new Money(100_00, Currency.USD)));
    }

    // --------------------------------------------------------------------- reserve

    [Fact]
    public void Reserve_LessThanAvailable_ReservesWithoutTouchingActualBalance()
    {
        var account = AnAccount(1000_00);

        var hold = account.Reserve(Gbp(500_00), Guid.NewGuid());

        Assert.Equal(Gbp(1000_00), account.ActualBalance);
        Assert.Equal(Gbp(500_00), account.ReservedBalance);
        Assert.Equal(Gbp(500_00), account.AvailableBalance);
        Assert.Equal(HoldState.Active, hold.HoldState);
        Assert.Equal(account.Id, hold.AccountId);
        Assert.Single(account.Holds);
    }

    [Fact]
    public void Reserve_ExactlyAvailable_SucceedsAndLeavesNothingAvailable()
    {
        var account = AnAccount(1000_00);

        account.Reserve(Gbp(1000_00), Guid.NewGuid());

        Assert.Equal(Money.Zero(Currency.GBP), account.AvailableBalance);
        Assert.Equal(Gbp(1000_00), account.ReservedBalance);
    }

    [Fact]
    public void Reserve_OneMinorUnitMoreThanAvailable_ThrowsInvalidOperationException()
    {
        var account = AnAccount(1000_00);

        Assert.Throws<InvalidOperationException>(() => account.Reserve(Gbp(1000_01), Guid.NewGuid()));
    }

    [Fact]
    public void Reserve_MoreThanAvailableWhenOtherHoldsExist_ThrowsInvalidOperationException()
    {
        var account = AnAccount(1000_00);
        account.Reserve(Gbp(500_00), Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() => account.Reserve(Gbp(900_00), Guid.NewGuid()));
    }

    [Fact]
    public void Reserve_FailedAttempt_LeavesBalancesAndHoldsUntouched()
    {
        var account = AnAccount(1000_00);
        account.Reserve(Gbp(500_00), Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() => account.Reserve(Gbp(900_00), Guid.NewGuid()));

        Assert.Equal(Gbp(500_00), account.ReservedBalance);
        Assert.Equal(Gbp(500_00), account.AvailableBalance);
        Assert.Single(account.Holds);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Reserve_NonPositiveAmount_ThrowsArgumentException(long minorUnits)
    {
        var account = AnAccount(1000_00);

        Assert.Throws<ArgumentException>(() => account.Reserve(Gbp(minorUnits), Guid.NewGuid()));
    }

    [Fact]
    public void Reserve_DifferentCurrency_ThrowsArgumentException()
    {
        var account = AnAccount(1000_00);

        Assert.Throws<ArgumentException>(
            () => account.Reserve(new Money(100_00, Currency.USD), Guid.NewGuid()));
    }

    [Fact]
    public void Reserve_EmptyIdempotencyKey_ThrowsArgumentException()
    {
        var account = AnAccount(1000_00);

        Assert.Throws<ArgumentException>(() => account.Reserve(Gbp(100_00), Guid.Empty));
    }

    [Fact]
    public void Reserve_SameKeyAndAmountTwice_ReturnsTheOriginalHoldAndReservesOnce()
    {
        var account = AnAccount(1000_00);
        var key = Guid.NewGuid();

        var first = account.Reserve(Gbp(500_00), key);
        var second = account.Reserve(Gbp(500_00), key);

        Assert.Same(first, second);
        Assert.Single(account.Holds);
        Assert.Equal(Gbp(500_00), account.ReservedBalance);
        Assert.Equal(Gbp(500_00), account.AvailableBalance);
    }

    [Fact]
    public void Reserve_ReplayAfterBalanceExhausted_StillReturnsTheOriginalHold()
    {
        var account = AnAccount(1000_00);
        var key = Guid.NewGuid();

        var first = account.Reserve(Gbp(500_00), key);
        account.Reserve(Gbp(500_00), Guid.NewGuid()); // nothing available now

        var replay = account.Reserve(Gbp(500_00), key);

        Assert.Same(first, replay);
        Assert.Equal(2, account.Holds.Count);
        Assert.Equal(Gbp(1000_00), account.ReservedBalance);
    }

    [Fact]
    public void Reserve_SameKeyDifferentAmount_ThrowsInvalidOperationException()
    {
        var account = AnAccount(1000_00);
        var key = Guid.NewGuid();
        account.Reserve(Gbp(500_00), key);

        Assert.Throws<InvalidOperationException>(() => account.Reserve(Gbp(5_00), key));

        Assert.Single(account.Holds);
        Assert.Equal(Gbp(500_00), account.ReservedBalance);
    }

    // --------------------------------------------------------------------- release

    [Fact]
    public void Release_ActiveHold_ReturnsFundsToAvailableWithoutTouchingActualBalance()
    {
        var account = AnAccount(1000_00);
        var key = Guid.NewGuid();
        account.Reserve(Gbp(500_00), key);

        account.Release(key);

        Assert.Equal(Gbp(1000_00), account.ActualBalance);
        Assert.Equal(Money.Zero(Currency.GBP), account.ReservedBalance);
        Assert.Equal(Gbp(1000_00), account.AvailableBalance);
        Assert.Equal(HoldState.Released, account.Holds.Single().HoldState);
    }

    [Fact]
    public void Release_AlreadyReleased_IsANoOp()
    {
        var account = AnAccount(1000_00);
        var key = Guid.NewGuid();
        account.Reserve(Gbp(500_00), key);
        account.Release(key);

        account.Release(key);

        Assert.Equal(Money.Zero(Currency.GBP), account.ReservedBalance);
        Assert.Equal(Gbp(1000_00), account.AvailableBalance);
    }

    [Fact]
    public void Release_AlreadyCommitted_ThrowsInvalidOperationException()
    {
        var account = AnAccount(1000_00);
        var key = Guid.NewGuid();
        account.Reserve(Gbp(500_00), key);
        account.Commit(key);

        Assert.Throws<InvalidOperationException>(() => account.Release(key));

        Assert.Equal(Gbp(500_00), account.ActualBalance);
        Assert.Equal(Money.Zero(Currency.GBP), account.ReservedBalance);
    }

    [Fact]
    public void Release_UnknownKey_ThrowsInvalidOperationException()
    {
        var account = AnAccount(1000_00);

        Assert.Throws<InvalidOperationException>(() => account.Release(Guid.NewGuid()));
    }

    // ---------------------------------------------------------------------- commit

    [Fact]
    public void Commit_ActiveHold_ReducesActualAndReservedLeavingAvailableUnchanged()
    {
        var account = AnAccount(1000_00);
        var key = Guid.NewGuid();
        account.Reserve(Gbp(500_00), key);
        var availableBeforeCommit = account.AvailableBalance;

        account.Commit(key);

        Assert.Equal(Gbp(500_00), account.ActualBalance);
        Assert.Equal(Money.Zero(Currency.GBP), account.ReservedBalance);
        Assert.Equal(availableBeforeCommit, account.AvailableBalance);
        Assert.Equal(HoldState.Committed, account.Holds.Single().HoldState);
    }

    [Fact]
    public void Commit_AlreadyCommitted_IsANoOp()
    {
        var account = AnAccount(1000_00);
        var key = Guid.NewGuid();
        account.Reserve(Gbp(500_00), key);
        account.Commit(key);

        account.Commit(key);

        Assert.Equal(Gbp(500_00), account.ActualBalance);
        Assert.Equal(Money.Zero(Currency.GBP), account.ReservedBalance);
    }

    [Fact]
    public void Commit_AlreadyReleased_ThrowsInvalidOperationException()
    {
        var account = AnAccount(1000_00);
        var key = Guid.NewGuid();
        account.Reserve(Gbp(500_00), key);
        account.Release(key);

        Assert.Throws<InvalidOperationException>(() => account.Commit(key));

        Assert.Equal(Gbp(1000_00), account.ActualBalance);
    }

    [Fact]
    public void Commit_UnknownKey_ThrowsInvalidOperationException()
    {
        var account = AnAccount(1000_00);

        Assert.Throws<InvalidOperationException>(() => account.Commit(Guid.NewGuid()));
    }

    // ------------------------------------------------------------------ invariants

    [Fact]
    public void MixedSequence_ReservedBalanceAlwaysEqualsTheSumOfActiveHolds()
    {
        var account = AnAccount(1000_00);
        var toCommit = Guid.NewGuid();
        var toRelease = Guid.NewGuid();
        var toLeaveActive = Guid.NewGuid();

        account.Reserve(Gbp(100_00), toCommit);
        Assert.Equal(SumOfActiveHolds(account), account.ReservedBalance);

        account.Reserve(Gbp(200_00), toRelease);
        Assert.Equal(SumOfActiveHolds(account), account.ReservedBalance);

        account.Reserve(Gbp(300_00), toLeaveActive);
        Assert.Equal(SumOfActiveHolds(account), account.ReservedBalance);

        account.Commit(toCommit);
        Assert.Equal(SumOfActiveHolds(account), account.ReservedBalance);

        account.Release(toRelease);
        Assert.Equal(SumOfActiveHolds(account), account.ReservedBalance);

        Assert.Equal(Gbp(300_00), account.ReservedBalance);
        Assert.Equal(Gbp(900_00), account.ActualBalance);
        Assert.Equal(Gbp(600_00), account.AvailableBalance);
    }

    [Fact]
    public void AvailableBalance_NeverGoesNegativeAcrossRepeatedReserveAndRelease()
    {
        var account = AnAccount(1000_00);

        for (var i = 0; i < 20; i++)
        {
            var key = Guid.NewGuid();
            account.Reserve(Gbp(900_00), key);
            Assert.False(account.AvailableBalance.IsNegative);

            if (i % 2 == 0)
            {
                account.Release(key);
            }
            else
            {
                account.Commit(key);
                account.Credit(Gbp(900_00));
            }

            Assert.False(account.AvailableBalance.IsNegative);
        }
    }
}