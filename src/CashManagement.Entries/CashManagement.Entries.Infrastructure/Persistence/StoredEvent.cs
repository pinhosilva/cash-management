namespace CashManagement.Entries.Infrastructure.Persistence;

/// <summary>Linha do event store (append-only). Único por (AggregateId, Version).</summary>
public sealed class StoredEvent
{
    public Guid EventId { get; set; }

    public Guid AggregateId { get; set; }

    public int Version { get; set; }

    public string Type { get; set; } = null!;

    public string Data { get; set; } = null!;

    public DateTime OccurredAt { get; set; }
}
