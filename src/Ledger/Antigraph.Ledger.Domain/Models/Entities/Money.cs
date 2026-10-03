using System;

namespace Antigraph.Ledger.Domain.Models.Entities;

public readonly record struct Money(long Amount, string CurrencyCode)
{
     public static Money operator +(Money m1, Money m2)
    {
        if (m1.CurrencyCode != m2.CurrencyCode)
        {
            throw new ArgumentException("Cannot add Money with different currencies.");
        }
        return new Money(m1.Amount + m2.Amount, m1.CurrencyCode);
    }
  
}




