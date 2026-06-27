using System.Text.Json;
using CashManagement.Entries.Domain.Persistence;
using CashManagement.Entries.Domain.SeedWork;
using CashManagement.Entries.Infrastructure.Messaging;
using CashManagement.Entries.Infrastructure.Persistence.Models;
using CashManagement.Entries.Infrastructure.Serialization;
using Microsoft.EntityFrameworkCore;

namespace CashManagement.Entries.Infrastructure.Persistence;

/// <summary>
/// Repositório de agregados sobre EF Core (SQL Server). <see cref="Add"/> encena,
/// para cada evento não-commitado, uma linha no event store <b>e</b> a linha de
/// outbox (envelope §4.3) — de forma genérica, num único lugar. Não commita
/// (isso é do <see cref="IUnitOfWork"/>). A unicidade de (AggregateId, Version)
/// garante append-only e concorrência otimista.
/// </summary>
public sealed class Repository : IRepository
{
    private const int EventSchemaVersion = 1;

    private readonly EntriesDbContext _db;
    private readonly EventSerializer _serializer;

    public Repository(EntriesDbContext db, EventSerializer serializer)
    {
        _db = db;
        _serializer = serializer;
    }

    public void Add(AggregateRoot aggregate)
    {
        var uncommitted = aggregate.UncommittedEvents.ToList();
        if (uncommitted.Count == 0)
        {
            return;
        }

        var version = aggregate.Version - uncommitted.Count;
        var createdAt = DateTime.UtcNow;

        foreach (var @event in uncommitted)
        {
            version++;
            var eventId = Guid.NewGuid();
            var type = @event.GetType().Name;

            _db.Events.Add(new StoredEvent
            {
                EventId = eventId,
                AggregateId = aggregate.Id,
                Version = version,
                Type = type,
                Data = _serializer.Serialize(@event),
                OccurredAt = @event.OccurredAt,
            });

            var envelope = new OutboxEnvelope(
                new EventInfo(eventId, type, EventSchemaVersion),
                new AggregateInfo(aggregate.Id, version),
                CorrelationId: null, // preenchido na borda (T08)
                InitiatedBy: null,   // preenchido na borda (T08)
                @event.OccurredAt,
                @event);

            _db.Outbox.Add(new OutboxMessage
            {
                Id = eventId,
                AggregateId = aggregate.Id,
                Type = type,
                Payload = JsonSerializer.Serialize(envelope, EventSerializer.Options),
                OccurredAt = @event.OccurredAt,
                CreatedAt = createdAt,
            });
        }

        aggregate.ClearUncommittedEvents();
    }

    public async Task<TAggregate?> GetAsync<TAggregate>(Guid id) where TAggregate : AggregateRoot
    {
        var stored = await _db.Events
            .AsNoTracking()
            .Where(e => e.AggregateId == id)
            .OrderBy(e => e.Version)
            .ToListAsync();

        if (stored.Count == 0)
        {
            return null;
        }

        var history = stored.Select(e => _serializer.Deserialize(e.Type, e.Data));
        return AggregateRoot.FromHistory<TAggregate>(history);
    }
}
