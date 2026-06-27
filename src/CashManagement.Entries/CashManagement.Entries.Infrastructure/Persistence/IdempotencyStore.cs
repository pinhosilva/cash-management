using CashManagement.Entries.Application.Interfaces;
using CashManagement.Entries.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace CashManagement.Entries.Infrastructure.Persistence;

/// <summary>
/// Implementação EF Core do <see cref="IIdempotencyStore"/>. A janela de 24h
/// (§4.3) é aplicada na leitura: chaves mais antigas são tratadas como ausentes.
/// O <see cref="Add"/> apenas encena a linha; o commit é do <c>IUnitOfWork</c>,
/// na mesma transação do evento — sem dual-write.
/// </summary>
public sealed class IdempotencyStore : IIdempotencyStore
{
    private static readonly TimeSpan Window = TimeSpan.FromHours(24);

    private readonly EntriesDbContext _db;

    public IdempotencyStore(EntriesDbContext db) => _db = db;

    public async Task<Guid?> FindAsync(string key, CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow - Window;
        var record = await _db.IdempotencyKeys
            .AsNoTracking()
            .SingleOrDefaultAsync(r => r.Key == key && r.CreatedAt >= cutoff, cancellationToken);

        return record?.EntryId;
    }

    public void Add(string key, Guid entryId) =>
        _db.IdempotencyKeys.Add(new IdempotencyRecord
        {
            Key = key,
            EntryId = entryId,
            CreatedAt = DateTime.UtcNow,
        });
}
