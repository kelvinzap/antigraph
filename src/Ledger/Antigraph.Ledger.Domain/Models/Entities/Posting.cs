namespace Antigraph.Ledger.Domain.Models.Entities;

public class Posting
{
    public Guid Id { get; init; }     
    public Money Amount { get; init; }
    public required Guid AccountId { get; init; }
    public Guid JournalId { get; init; }    
    public Direction Direction { get; init; }
    public DateTime CreatedAt { get; init; } 
}
