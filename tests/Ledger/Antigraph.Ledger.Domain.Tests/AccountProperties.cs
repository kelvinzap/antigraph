using System;

using Antigraph.Ledger.Domain.Models.Entities;

using FsCheck.Xunit;

namespace Antigraph.Ledger.Domain.Tests;

public class AccountProperties
{
    /// <summary>
    /// For any sequence of credits, reserves, releases and commits — valid or not —
    /// the account's balances stay consistent with the holds that produced them.
    /// </summary>
    [Property(MaxTest = 500)]
    public void Invariants_HoldAfterEveryOperation(byte[] opCodes, ushort[] amounts)
    {
        opCodes ??= Array.Empty<byte>();
        amounts ??= Array.Empty<ushort>();

        var account = new Account(Guid.NewGuid(), "Property Account", Currency.GBP);
        var totalCredited = Money.Zero(Currency.GBP);

        void CheckInvariants(string after)
        {
            // 1. The running total agrees with the holds it was derived from.
            Assert.Equal(SumOfHoldsInState(account, HoldState.Active), account.ReservedBalance);

            // 2. Money is conserved: everything paid in, minus everything committed out.
            var expectedActual = totalCredited - SumOfHoldsInState(account, HoldState.Committed);
            Assert.Equal(expectedActual, account.ActualBalance);

            // 3. No balance can be negative, so reserved can never exceed actual.
            Assert.False(account.ActualBalance.IsNegative, $"ActualBalance negative after {after}");
            Assert.False(account.ReservedBalance.IsNegative, $"ReservedBalance negative after {after}");
            Assert.False(account.AvailableBalance.IsNegative, $"AvailableBalance negative after {after}");
        }

        CheckInvariants("construction");

        var steps = Math.Min(opCodes.Length, amounts.Length);

        for (var i = 0; i < steps; i++)
        {
            var operation = opCodes[i] % 4;      // which method to call
            var selector = opCodes[i] / 4;       // which existing hold to target
            var amount = new Money(amounts[i], Currency.GBP);
            var description = $"step {i}: op {operation}, amount {amount}";

            try
            {
                switch (operation)
                {
                    case 0:
                        account.Credit(amount);
                        totalCredited += amount;   // only reached if the credit was accepted
                        break;

                    case 1:
                        account.Reserve(amount, Guid.NewGuid());
                        break;

                    case 2:
                        if (account.Holds.Count > 0)
                        {
                            account.Release(PickHold(account, selector).IdempotencyKey);
                        }

                        break;

                    default:
                        if (account.Holds.Count > 0)
                        {
                            account.Commit(PickHold(account, selector).IdempotencyKey);
                        }

                        break;
                }
            }
            catch (ArgumentException)
            {
                // Malformed input (zero amount, wrong currency). Rejection is correct behaviour;
                // the invariant check below proves nothing was mutated on the way out.
            }
            catch (InvalidOperationException)
            {
                // Illegal operation (insufficient funds, wrong hold state). Same reasoning.
            }

            CheckInvariants(description);
        }
    }

    private static Hold PickHold(Account account, int selector) =>
        account.Holds[selector % account.Holds.Count];

    /// <summary>Independent oracle — recomputed from the holds, never read from the account's totals.</summary>
    private static Money SumOfHoldsInState(Account account, HoldState state) =>
        account.Holds
            .Where(h => h.HoldState == state)
            .Select(h => h.Amount)
            .Aggregate(Money.Zero(account.Currency), (running, next) => running + next);
}