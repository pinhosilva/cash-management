namespace CashManagement.Entries.Domain.SeedWork;

/// <summary>
/// Marca um fato de domínio já ocorrido (Event Sourcing). Carrega o
/// identificador do agregado (stream) ao qual pertence.
/// </summary>
public interface IDomainEvent
{
    Guid AggregateId { get; }
}
