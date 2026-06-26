namespace CashManagement.Entries.Domain.SeedWork;

/// <summary>
/// Base dos eventos de domínio. Imutável (record) e portador do
/// <see cref="AggregateId"/> — o stream a que o evento pertence.
/// Eventos concretos (ex.: CreditPostedEvent) herdam desta base.
/// </summary>
public abstract record DomainEvent(Guid AggregateId) : IDomainEvent;
