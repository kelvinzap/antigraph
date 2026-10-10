using System;

namespace Antigraph.Ledger.Domain.Models.Entities;

public class Hold
{
    public Guid IdempotencyKey { get; init; }
    public Guid AccountId { get; init; }
    public Money Amount { get; init; }
    public HoldState HoldState { get; set; }
    public DateTimeOffset CreatedAt { get; internal set; }
}

public enum HoldState
{
    Active,
    Released,
    Committed,
    Expired
}
