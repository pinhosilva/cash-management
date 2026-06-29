namespace CashManagement.Entries.Infrastructure.Messaging;

/// <summary>
/// Envelope publicado no Kafka (contrato da §4.3). Serializado em camelCase no
/// <c>Payload</c> da outbox. <c>CorrelationId</c>/<c>InitiatedBy</c> são
/// preenchidos na borda (T08); a key da mensagem é o <c>Aggregate.Id</c>.
/// </summary>
public sealed record OutboxEnvelope(EventInfo Event, AggregateInfo Aggregate, string? CorrelationId, string? InitiatedBy, DateTime OccurredAt, object Data);

public sealed record EventInfo(Guid Id, string Type, int Version);

public sealed record AggregateInfo(Guid Id, int Version);
