using System;

namespace Antigraph.Ledger.Domain.Models.Entities;

public readonly record struct Money(long MinorUnits, Currency Currency)
{
   public static Money Zero(Currency currency) => new(0, currency);

    public bool IsZero     => MinorUnits == 0;
    public bool IsPositive => MinorUnits > 0;
    public bool IsNegative => MinorUnits < 0;

    public static Money operator +(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return new Money(checked(left.MinorUnits + right.MinorUnits), left.Currency);
    }

    public static Money operator -(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return new Money(checked(left.MinorUnits - right.MinorUnits), left.Currency);
    }

    public static Money operator -(Money value) =>
        new(checked(-value.MinorUnits), value.Currency);

    public static bool operator <(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return left.MinorUnits < right.MinorUnits;
    }

    public static bool operator >(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return left.MinorUnits > right.MinorUnits;
    }

    public static bool operator <=(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return left.MinorUnits <= right.MinorUnits;
    }

    public static bool operator >=(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return left.MinorUnits >= right.MinorUnits;
    }

    private static void EnsureSameCurrency(Money left, Money right)
    {
        if (left.Currency != right.Currency)
        {
            throw new ArgumentException(
                $"Currency mismatch: cannot combine {left.Currency} with {right.Currency}.",
                nameof(right));
        }
    }

    public override string ToString()
    {
        var exponent = Currency.MinorUnitExponent();
        var divisor = 1L;
        for (var i = 0; i < exponent; i++) divisor *= 10;
        return $"{(MinorUnits / (decimal)divisor).ToString($"F{exponent}")} {Currency}";
    }
  
}




