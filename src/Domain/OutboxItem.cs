namespace Domain;

public sealed class OutboxItem
{
    public Guid Id { get; init; }
    public ProductEntry Entry { get; init; }
    public SyncState State { get; set; }
    public int Attempts { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? LastTriedAt { get; set; }

    public OutboxItem(ProductEntry entry)
    {
        Entry = entry;
        Id = entry.Id;
        CreatedAt = DateTimeOffset.UtcNow;
        State = SyncState.Pending;
    }
}

public enum SyncState
{
    Pending = 0,
    InFlight = 1,
    Completed = 2,
    Failed = 3
}
