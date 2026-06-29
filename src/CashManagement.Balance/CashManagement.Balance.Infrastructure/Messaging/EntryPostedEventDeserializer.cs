using System.Text.Json;
using CashManagement.Balance.Application.Features.BalanceProjection;

namespace CashManagement.Balance.Infrastructure.Messaging;

/// <summary>
/// Desserializa o envelope §4.3 (camelCase) e roteia por <c>event.type</c>. Projeta
/// <c>CreditPostedEvent</c> (soma) e <c>DebitPostedEvent</c> (subtrai); outros tipos
/// viram <c>null</c> (ignorados). Mantém o consumer fino e testável de forma isolada.
/// </summary>
public static class EntryPostedEventDeserializer
{
    public const string CreditPostedEventType = "CreditPostedEvent";
    public const string DebitPostedEventType = "DebitPostedEvent";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Converte o payload em <see cref="EntryPosted"/> se for um crédito/débito válido;
    /// caso contrário devolve <c>null</c> (tipo fora de escopo ou payload incompleto).
    /// </summary>
    public static EntryPosted? TryParse(string payload)
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

        if (envelope?.Event is null || envelope.Data?.Amount is null)
        {
            return null;
        }

        var kind = envelope.Event.Type switch
        {
            CreditPostedEventType => (EntryKind?)EntryKind.Credit,
            DebitPostedEventType => EntryKind.Debit,
            _ => null,
        };

        if (kind is null)
        {
            return null;
        }

        return new EntryPosted(
            envelope.Event.Id,
            kind.Value,
            envelope.Data.Amount.Amount,
            envelope.OccurredAt,
            envelope.CorrelationId);
    }
}
