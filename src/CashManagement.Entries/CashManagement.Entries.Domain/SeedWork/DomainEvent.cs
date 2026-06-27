namespace CashManagement.Entries.Domain.SeedWork;

/// <summary>
/// Base dos eventos de domínio. Imutável (record), portador do
/// <see cref="AggregateId"/> (stream) e do <see cref="OccurredAt"/> (UTC).
/// Eventos concretos (ex.: CreditPostedEvent) herdam desta base.
/// </summary>
public abstract record DomainEvent(Guid AggregateId, DateTime OccurredAt) : IDomainEvent;
