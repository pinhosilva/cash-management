using System.Text.Json;
using CashManagement.Balance.Application.Features.BalanceProjection;

namespace CashManagement.Balance.Infrastructure.Messaging;

/// <summary>
/// Desserializa o envelope §4.3 (camelCase) e roteia por <c>event.type</c>. Nesta
/// fatia só <c>CreditPostedEvent</c> é projetado; outros tipos viram <c>null</c>
/// (ignorados). Mantém o consumer fino e testável de forma isolada.
/// </summary>
public static class CreditPostedEventDeserializer
{
    public const string CreditPostedEventType = "CreditPostedEvent";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Converte o payload em <see cref="CreditPosted"/> se for um crédito válido;
    /// caso contrário devolve <c>null</c> (tipo fora de escopo ou payload incompleto).
    /// </summary>
    public static CreditPosted? TryParseCredit(string payload)
    {
        EventEnvelope? envelope;
        try
        {
            envelope = JsonSerializer.Deserialize<EventEnvelope>(payload, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }

        if (envelope?.Event is null ||
            !string.Equals(envelope.Event.Type, CreditPostedEventType, StringComparison.Ordinal) ||
            envelope.Data?.Amount is null)
        {
            return null;
        }

        return new CreditPosted(
            envelope.Event.Id,
            envelope.Data.Amount.Amount,
            envelope.OccurredAt,
            envelope.CorrelationId);
    }
}
