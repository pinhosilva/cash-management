using CashManagement.Entries.Domain.SeedWork;

namespace CashManagement.Entries.Domain.Persistence;

/// <summary>
/// Repositório de agregados (domain-neutral): o caso de uso só declara "persista
/// este agregado" / "carregue este agregado". O mecanismo de Event Sourcing —
/// gravar os eventos no event store + a linha de outbox (§5.9), e depois publicar
/// via relay — fica por baixo dos panos, genérico para qualquer agregado/evento.
/// O commit é do <see cref="IUnitOfWork"/>, na fronteira do caso de uso.
/// </summary>
public interface IRepository
{
    /// <summary>Encena os eventos não-commitados do agregado (event store + outbox). Não commita.</summary>
    void Add(AggregateRoot aggregate);

    /// <summary>Reconstrói o agregado por replay dos seus eventos.</summary>
    Task<TAggregate?> GetAsync<TAggregate>(Guid id) where TAggregate : AggregateRoot;
}
