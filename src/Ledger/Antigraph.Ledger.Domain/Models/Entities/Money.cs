using System;

namespace Antigraph.Ledger.Domain.Models.Entities;

public readonly record struct Money(long MinorUnits, Currency Currency)
{
     public static Money operator +(Money m1, Money m2)
    {
        if (!m1.Currency.Equals(m2.Currency))
        {
            throw new ArgumentException("Cannot add Money with different currencies.");
        }
        return new Money(m1.MinorUnits + m2.MinorUnits, m1.Currency);
    }

    public static Money operator -(Money value) => new(-value.MinorUnits, value.Currency);
  
}




