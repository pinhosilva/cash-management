using CashManagement.Entries.Application.Interfaces;
using CashManagement.Entries.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CashManagement.Entries.Infrastructure.Persistence;

/// <summary>
/// Implementação EF Core do <see cref="IIdempotencyStore"/>. A janela de dedup
/// (§4.3, default 24h — config <see cref="IdempotencyOptions"/>) é aplicada na
/// leitura: chaves mais antigas são tratadas como ausentes. O <see cref="Add"/>
/// apenas encena a linha; o commit é do <c>IUnitOfWork</c>, na mesma transação do
/// evento — sem dual-write.
/// </summary>
public sealed class IdempotencyStore : IIdempotencyStore
{
    private readonly EntriesDbContext _db;
    private readonly TimeSpan _window;

    public IdempotencyStore(EntriesDbContext db, IOptions<IdempotencyOptions> options)
    {
        _db = db;
        _window = TimeSpan.FromHours(options.Value.WindowHours);
    }

    public async Task<Guid?> FindAsync(string key, CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow - _window;
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
