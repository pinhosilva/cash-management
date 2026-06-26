using CashManagement.Entries.Domain.Aggregates;

namespace CashManagement.Entries.Domain.Repositories;

/// <summary>
/// Abstrai o event store do agregado <see cref="Entry"/>. O domínio não sabe
/// que por trás é SQL Server. A implementação (Infrastructure) grava os eventos
/// + a linha de outbox na mesma transação (§5.9) e reconstrói por replay.
/// </summary>
public interface IEntryRepository
{
    Task SaveAsync(Entry entry);

    Task<Entry?> GetByIdAsync(Guid id);
}
