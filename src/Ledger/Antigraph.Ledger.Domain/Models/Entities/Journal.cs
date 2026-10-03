namespace Antigraph.Ledger.Domain.Models.Entities;

public enum Direction
{
    CR,
    DR
}

public class Journal
{

    public Journal(List<Posting> entries, string paymentId)
    {
        if (entries == null || entries.Count < 2)
        {
            throw new ArgumentException("Entries cannot be less than 2 .");
        }

        if (!entries.Exists(e => e.Direction == Direction.CR) || !entries.Exists(e => e.Direction == Direction.DR))
        {
            throw new ArgumentException("Entries must contain at least one credit and one debit entry.");
        }

        var net = entries
        .Select(e => e.Direction == Direction.CR ? e.Amount : -e.Amount)
        .Aggregate((a, b) => a + b);

        if (net.MinorUnits != 0)
            throw new ArgumentException("Entries must be balanced.");



        DateTime now = DateTime.UtcNow;
        this.Id = Guid.NewGuid();
        this.Postings = entries.Select(e => new Posting
        {
            Id = Guid.NewGuid(),        
            Amount = e.Amount,
            AccountId = e.AccountId,
            JournalId = this.Id,
            Direction = e.Direction,
            CreatedAt = now
        }).ToList();
        this.IdempotencyKey = $"payment:{paymentId}:commit";
        this.CreatedAt = now;
    }
    public Guid Id { get; init; }
    //payment:{paymentId}:commit
    public string IdempotencyKey { get; init; }
    public IReadOnlyList<Posting> Postings { get; init; }
    public DateTime CreatedAt { get; init; }

}
