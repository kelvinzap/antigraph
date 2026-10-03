using System;

namespace Antigraph.Ledger.Domain.Models.Entities;

public enum Currency { GBP, USD, EUR, JPY, KWD, NGN }

public static class CurrencyExtensions
{
    public static int MinorUnitExponent(this Currency currency) => currency switch
    {
        Currency.JPY => 0,
        Currency.KWD => 3,
        _ => 2
    };
}
