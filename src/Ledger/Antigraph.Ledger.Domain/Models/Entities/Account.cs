using System;

namespace Antigraph.Ledger.Domain.Models.Entities;

public class Account
{
    private readonly List<Hold> _holds = new();
    private readonly TimeProvider _clock;

    public Account(Guid id, string name, Currency currency, TimeProvider? clock = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Account id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Account name is required.", nameof(name));
        }

        _clock = clock ?? TimeProvider.System;

        Id = id;
        AccountName = name;
        Currency = currency;
        ActualBalance = Money.Zero(currency);
        ReservedBalance = Money.Zero(currency);
        CreatedAt = _clock.GetUtcNow();
    }

    public Guid Id { get; init; }
    public string AccountName { get; init; }
    public Currency Currency { get; init; }
    public DateTimeOffset CreatedAt { get; init; }

    public Money ActualBalance { get; private set; }
    public Money ReservedBalance { get; private set; }
    public Money AvailableBalance => ActualBalance - ReservedBalance;

    public IReadOnlyList<Hold> Holds => _holds;

    /// <summary>Puts funds into the account. Temporary: the ledger takes this over in week 2.</summary>
    public void Credit(Money amount)
    {
        EnsureCurrency(amount);

        if (!amount.IsPositive)
        {
            throw new ArgumentException("Credit amount must be greater than zero.", nameof(amount));
        }

        ActualBalance += amount;
    }

    public Hold Reserve(Money amount, Guid idempotencyKey)
    {
        if (idempotencyKey == Guid.Empty)
        {
            throw new ArgumentException("Idempotency key is required.", nameof(idempotencyKey));
        }

        // Replay check first: a retry must not be judged against today's balance.
        var existing = _holds.Find(h => h.IdempotencyKey == idempotencyKey);
        if (existing is not null)
        {
            if (existing.Amount != amount)
            {
                throw new InvalidOperationException(
                    $"Idempotency key {idempotencyKey} was already used to reserve {existing.Amount}; " +
                    $"it cannot be reused for {amount}.");
            }

            return existing;
        }

        EnsureCurrency(amount);

        if (!amount.IsPositive)
        {
            throw new ArgumentException("Amount to reserve must be greater than zero.", nameof(amount));
        }

        if (AvailableBalance < amount)
        {
            throw new InvalidOperationException(
                $"Insufficient available balance: {AvailableBalance} available, {amount} requested.");
        }

        var hold = new Hold
        {
            IdempotencyKey = idempotencyKey,
            Amount = amount,
            AccountId = Id,
            CreatedAt = _clock.GetUtcNow(),
            HoldState = HoldState.Active
        };

        _holds.Add(hold);
        ReservedBalance += amount;

        return hold;
    }

    public void Release(Guid idempotencyKey)
    {
        var hold = RequireHold(idempotencyKey);

        if (hold.HoldState == HoldState.Released)
        {
            return; // replay of a release that already happened
        }

        if (hold.HoldState != HoldState.Active)
        {
            throw new InvalidOperationException(
                $"Hold {idempotencyKey} is {hold.HoldState} and cannot be released.");
        }

        ReservedBalance -= hold.Amount;
        hold.HoldState = HoldState.Released;
    }

    public void Commit(Guid idempotencyKey)
    {
        var hold = RequireHold(idempotencyKey);

        if (hold.HoldState == HoldState.Committed)
        {
            return; // replay of a commit that already happened
        }

        if (hold.HoldState != HoldState.Active)
        {
            throw new InvalidOperationException(
                $"Hold {idempotencyKey} is {hold.HoldState} and cannot be committed.");
        }

        ActualBalance -= hold.Amount;
        ReservedBalance -= hold.Amount;
        hold.HoldState = HoldState.Committed;
    }

    private Hold RequireHold(Guid idempotencyKey) =>
        _holds.Find(h => h.IdempotencyKey == idempotencyKey)
        ?? throw new InvalidOperationException($"No hold found for idempotency key {idempotencyKey}.");

    private void EnsureCurrency(Money amount)
    {
        if (amount.Currency != Currency)
        {
            throw new ArgumentException(
                $"Amount currency {amount.Currency} does not match account currency {Currency}.",
                nameof(amount));
        }
    }
}