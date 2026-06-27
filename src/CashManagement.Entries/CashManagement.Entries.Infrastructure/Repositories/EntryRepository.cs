using System.Text.Json;
using CashManagement.Entries.Domain.Aggregates;
using CashManagement.Entries.Domain.Repositories;
using CashManagement.Entries.Domain.SeedWork;
using CashManagement.Entries.Infrastructure.Messaging;
using CashManagement.Entries.Infrastructure.Persistence;
using CashManagement.Entries.Infrastructure.Serialization;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CashManagement.Entries.Infrastructure.Repositories;

/// <summary>
/// Event store sobre SQL Server (EF Core). <see cref="SaveAsync"/> grava os
/// eventos não-commitados + a linha de outbox na MESMA transação (Unit of Work,
/// §5.9); a unicidade de (AggregateId, Version) garante append-only e
/// concorrência otimista.
/// </summary>
public sealed class EntryRepository : IEntryRepository
{
    private const int SqlUniqueViolation = 2627;
    private const int SqlUniqueIndexViolation = 2601;
    private const int EventSchemaVersion = 1;

    private readonly EntriesDbContext _db;
    private readonly EventSerializer _serializer;

    public EntryRepository(EntriesDbContext db, EventSerializer serializer)
    {
        _db = db;
        _serializer = serializer;
    }

    public async Task SaveAsync(Entry entry)
    {
        var uncommitted = entry.UncommittedEvents.ToList();
        if (uncommitted.Count == 0)
        {
            return;
        }

        var expectedVersion = entry.Version - uncommitted.Count;
        var createdAt = DateTime.UtcNow;

        await using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            var version = expectedVersion;
            foreach (var @event in uncommitted)
            {
                version++;
                var eventId = Guid.NewGuid();
                var type = @event.GetType().Name;

                _db.Events.Add(new StoredEvent
                {
                    EventId = eventId,
                    AggregateId = entry.Id,
                    Version = version,
                    Type = type,
                    Data = _serializer.Serialize(@event),
                    OccurredAt = @event.OccurredAt,
                });

                var envelope = new OutboxEnvelope(
                    new EventInfo(eventId, type, EventSchemaVersion),
                    new AggregateInfo(entry.Id, version),
                    CorrelationId: null, // preenchido na borda (T08)
                    InitiatedBy: null,   // preenchido na borda (T08)
                    @event.OccurredAt,
                    @event);

                _db.Outbox.Add(new OutboxMessage
                {
                    Id = eventId,
                    AggregateId = entry.Id,
                    Type = type,
                    Payload = JsonSerializer.Serialize(envelope, EventSerializer.Options),
                    OccurredAt = @event.OccurredAt,
                    CreatedAt = createdAt,
                });
            }

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();
            entry.ClearUncommittedEvents();
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            await transaction.RollbackAsync();
            throw new ConcurrencyConflictException(entry.Id, expectedVersion, ex);
        }
    }

    public async Task<Entry?> GetByIdAsync(Guid id)
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
        return AggregateRoot.FromHistory<Entry>(history);
    }

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is SqlException { Number: SqlUniqueViolation or SqlUniqueIndexViolation };
}
