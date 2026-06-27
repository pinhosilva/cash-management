using System.Text.Json;
using CashManagement.Entries.Domain.SeedWork;

namespace CashManagement.Entries.Infrastructure.Serialization;

/// <summary>
/// Serializa eventos de domínio ↔ JSON. Os tipos de evento são descobertos por
/// reflection no assembly de Domain <b>uma vez</b> (startup); o runtime depois é
/// só lookup em dicionário + System.Text.Json. Evento novo não exige mudança aqui.
/// </summary>
/// <remarks>
/// Reflection na descoberta de tipo é uso normal de serialização (fora do hot
/// path) — o guardrail "sem reflection" da seed work é escopado ao caminho quente
/// (igualdade de VO, roteamento de evento), não a isto.
/// </remarks>
public sealed class EventSerializer
{
    // Instância única reutilizada (recomendação do System.Text.Json — cacheia metadados).
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private static readonly IReadOnlyDictionary<string, Type> EventTypes =
        typeof(IDomainEvent).Assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false } && typeof(IDomainEvent).IsAssignableFrom(t))
            .ToDictionary(t => t.Name);

    public string Serialize(IDomainEvent @event) =>
        JsonSerializer.Serialize(@event, @event.GetType(), Options);

    public IDomainEvent Deserialize(string type, string json)
    {
        if (!EventTypes.TryGetValue(type, out var clrType))
        {
            throw new NotSupportedException($"Unknown event type '{type}'.");
        }

        return (IDomainEvent)JsonSerializer.Deserialize(json, clrType, Options)!;
    }
}
