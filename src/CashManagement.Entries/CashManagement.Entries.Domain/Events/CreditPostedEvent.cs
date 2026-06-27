using CashManagement.Entries.Domain.SeedWork;
using CashManagement.Entries.Domain.ValueObjects;

namespace CashManagement.Entries.Domain.Events;

/// <summary>
/// Fato consumado: um crédito foi registrado. Carrega o stream
/// (<see cref="DomainEvent.AggregateId"/>), o valor e o instante (UTC) da ocorrência.
/// </summary>
public sealed record CreditPostedEvent(Guid AggregateId, Money Amount, DateTime OccurredAt)
    : DomainEvent(AggregateId, OccurredAt);
