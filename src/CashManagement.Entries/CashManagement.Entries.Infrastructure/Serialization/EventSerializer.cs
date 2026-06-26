using System.Text.Json;
using CashManagement.Entries.Domain.Events;
using CashManagement.Entries.Domain.SeedWork;
using CashManagement.Entries.Domain.ValueObjects;

namespace CashManagement.Entries.Infrastructure.Serialization;

/// <summary>
/// Converte eventos de domínio ↔ JSON de wire. O schema de wire é desacoplado
/// dos tipos de domínio (DTOs próprios) — base para versionamento/upcasting (§FAQ).
/// </summary>
public sealed class EventSerializer
{
    // Instância única reutilizada (recomendação do System.Text.Json — cacheia metadados).
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public SerializedEvent Serialize(IDomainEvent @event) => @event switch
    {
        CreditPostedEvent e => new SerializedEvent(
            nameof(CreditPostedEvent),
            new CreditPostedData(new MoneyData(e.Amount.Amount, e.Amount.Currency)),
            e.OccurredAt),
        _ => throw new NotSupportedException($"Unknown event type '{@event.GetType().Name}'.")
    };

    public IDomainEvent Deserialize(Guid aggregateId, string type, string data, DateTime occurredAt) => type switch
    {
        nameof(CreditPostedEvent) => DeserializeCreditPosted(aggregateId, data, occurredAt),
        _ => throw new NotSupportedException($"Unknown event type '{type}'.")
    };

    private static CreditPostedEvent DeserializeCreditPosted(Guid aggregateId, string data, DateTime occurredAt)
    {
        var payload = JsonSerializer.Deserialize<CreditPostedData>(data, Options)
            ?? throw new InvalidOperationException("Invalid CreditPostedEvent payload.");

        return new CreditPostedEvent(
            aggregateId,
            Money.Of(payload.Amount.Amount, payload.Amount.Currency),
            occurredAt);
    }
}

/// <summary>Evento serializado: tipo, payload (data §4.3) e instante; schema v1.</summary>
public sealed record SerializedEvent(string Type, object Data, DateTime OccurredAt, int SchemaVersion = 1);

// DTOs do schema de wire (não são os tipos de domínio).
public sealed record CreditPostedData(MoneyData Amount);

public sealed record MoneyData(decimal Amount, string Currency);
