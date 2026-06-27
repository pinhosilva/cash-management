namespace CashManagement.Entries.Domain.SeedWork;

/// <summary>
/// Marca um fato de domínio já ocorrido (Event Sourcing). Carrega o
/// identificador do agregado (stream) e o instante (UTC) da ocorrência.
/// </summary>
public interface IDomainEvent
{
    Guid AggregateId { get; }

    DateTime OccurredAt { get; }
}
