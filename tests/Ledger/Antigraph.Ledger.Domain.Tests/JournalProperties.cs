using System;
using System.Numerics;

using Antigraph.Ledger.Domain.Models.Entities;

using FsCheck;
using FsCheck.Xunit;

namespace Antigraph.Ledger.Domain.Tests;

public class JournalProperties
{

   [Property]
    public void Journal_is_accepted_only_when_legs_balance_exactly(
        long debit, long credit1, bool makeItBalance, long noise)
    {
        // Third leg either balances the journal, or is arbitrary.
        long credit2 = makeItBalance ? debit - credit1 : noise;

        var account = Guid.NewGuid();
        var postings = new List<Posting>
        {
            Leg(debit,   Direction.DR, account),
            Leg(credit1, Direction.CR, account),
            Leg(credit2, Direction.CR, account),
        };

        // Oracle: exact arithmetic. Cannot wrap, cannot lose precision.
        BigInteger exact = (BigInteger)credit1 + credit2 - debit;
        bool shouldBeAccepted = exact.IsZero;

        bool accepted;
        try
        {
            _ = new Journal(postings, "payment-1");
            accepted = true;
        }
        catch (ArgumentException)
        {
            accepted = false;
        }

        Assert.Equal(shouldBeAccepted, accepted);
    }

    private static Posting Leg(long minorUnits, Direction direction, Guid account) =>
        new()
        {
            Amount = new Money(minorUnits, Currency.GBP),
            Direction = direction,
            AccountId = account,
        };
}
