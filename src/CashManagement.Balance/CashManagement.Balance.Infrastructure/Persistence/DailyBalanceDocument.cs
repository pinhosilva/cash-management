using MongoDB.Bson.Serialization.Attributes;

namespace CashManagement.Balance.Infrastructure.Persistence;

/// <summary>
/// Documento do read model do saldo diário no MongoDB. O <c>_id</c> é o dia
/// (<c>yyyy-MM-dd</c>, UTC), o que torna o saldo do dia um único documento e permite
/// dedup + ajuste atômicos. <c>ProcessedEventIds</c> guarda as <c>event.id</c> já
/// aplicadas (marcador de dedup, §4.3/§6.3) — sem dual-write no consumo.
/// </summary>
public sealed class DailyBalanceDocument
{
    [BsonId]
    public string Date { get; set; } = default!;

    [BsonElement("totalCredits")]
    [BsonRepresentation(MongoDB.Bson.BsonType.Decimal128)]
    public decimal TotalCredits { get; set; }

    [BsonElement("totalDebits")]
    [BsonRepresentation(MongoDB.Bson.BsonType.Decimal128)]
    public decimal TotalDebits { get; set; }

    [BsonElement("balance")]
    [BsonRepresentation(MongoDB.Bson.BsonType.Decimal128)]
    public decimal Balance { get; set; }

    [BsonElement("processedEventIds")]
    public List<string> ProcessedEventIds { get; set; } = [];
}
