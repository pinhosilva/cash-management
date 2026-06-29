namespace CashManagement.Entries.Infrastructure.Persistence.Models;

/// <summary>
/// Mensagem da Transactional Outbox (§5.9). Gravada na mesma transação do evento;
/// o relay (T07) lê as não-processadas e publica no Kafka.
/// </summary>
public sealed class OutboxMessage
{
    /// <summary>Id da mensagem = event.id (chave de dedup no consumo, §4.3).</summary>
    public Guid Id { get; set; }

    /// <summary>Usado como key/partition key da mensagem Kafka (§4.3).</summary>
    public Guid AggregateId { get; set; }

    public string Type { get; set; } = null!;

    /// <summary>Envelope §4.3 serializado (JSON) — o que será publicado.</summary>
    public string Payload { get; set; } = null!;

    public DateTime OccurredAt { get; set; }

    public DateTime CreatedAt { get; set; }

    /// <summary>Nulo = ainda não publicado.</summary>
    public DateTime? ProcessedAt { get; set; }
}
