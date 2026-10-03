using System;

namespace Antigraph.Ledger.Domain.Models.Entities;

public readonly record struct Money(long Amount, Currency Currency)
{
     public static Money operator +(Money m1, Money m2)
    {
        if (!m1.Currency.Equals(m2.Currency))
        {
            throw new ArgumentException("Cannot add Money with different currencies.");
        }
        return new Money(m1.Amount + m2.Amount, m1.Currency);
    }
  
}




