using System;
using System.Numerics;

using Antigraph.Ledger.Domain.Models.Entities;

using FsCheck;
using FsCheck.Xunit;

namespace Antigraph.Ledger.Domain.Tests;

public class MoneyProperties
{
    [Property]
    public bool Addition_matches_exact_arithmetic_or_refuses(DoNotSize<long> a, DoNotSize<long> b)
    {
        BigInteger exact = (BigInteger)a.Item + (BigInteger)b.Item;
        bool representable = exact >= long.MinValue && exact <= long.MaxValue;

        try
        {
            var result = new Money(a.Item, Currency.GBP) + new Money(b.Item, Currency.GBP);
            return representable && result.MinorUnits == exact;
        }
        catch (OverflowException)
        {
            return !representable;
        }
    }

    [Fact]
    public void Adding_beyond_MaxValue_throws()
    {

        // Arrange
        var before = new Money(long.MaxValue, Currency.GBP);


        //Act & Assert
        Assert.Throws<OverflowException>(() => before + new Money(1, Currency.GBP));
    }
}
