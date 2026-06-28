namespace CashManagement.Balance.Infrastructure.Messaging;

/// <summary>
/// Modelo de leitura do envelope publicado pelo Entries (contrato §4.3). DTOs
/// <b>próprios</b> do Balance — os serviços são desacoplados (§4.2), não se referencia
/// o projeto Entries. Desserializado em camelCase (<c>JsonSerializerDefaults.Web</c>).
/// </summary>
public sealed record EventEnvelope(
    EventDescriptor? Event,
    AggregateDescriptor? Aggregate,
    string? CorrelationId,
    string? InitiatedBy,
    DateTime OccurredAt,
    CreditData? Data);

public sealed record EventDescriptor(string Id, string Type, int Version);

public sealed record AggregateDescriptor(string Id, int Version);

/// <summary>Payload de <c>data</c> de um <c>CreditPostedEvent</c> (§4.3).</summary>
public sealed record CreditData(string AggregateId, MoneyData? Amount, DateTime OccurredAt);

public sealed record MoneyData(decimal Amount, string Currency);
