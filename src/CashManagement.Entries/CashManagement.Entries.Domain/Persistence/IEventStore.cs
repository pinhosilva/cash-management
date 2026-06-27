using CashManagement.Entries.Domain.SeedWork;

namespace CashManagement.Entries.Domain.Persistence;

/// <summary>
/// Event store genérico: encena (não commita) os eventos não-commitados de um
/// agregado — junto com a linha de outbox (§5.9) — e reidrata por replay.
/// É genérico (qualquer <see cref="AggregateRoot"/>); o commit é do
/// <see cref="IUnitOfWork"/>.
/// </summary>
public interface IEventStore
{
    /// <summary>Encena os eventos do agregado + outbox na unidade de trabalho atual. Não commita.</summary>
    void Append(AggregateRoot aggregate);

    /// <summary>Reconstrói o agregado por replay dos seus eventos.</summary>
    Task<TAggregate?> LoadAsync<TAggregate>(Guid id) where TAggregate : AggregateRoot;
}
