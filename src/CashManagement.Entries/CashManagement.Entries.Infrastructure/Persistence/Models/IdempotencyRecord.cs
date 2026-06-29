namespace CashManagement.Entries.Infrastructure.Persistence.Models;

/// <summary>
/// Registro de deduplicação de escrita (§4.3). Guarda, por <c>Idempotency-Key</c>,
/// o id do lançamento criado na primeira gravação. Um retry com a mesma chave
/// (dentro da janela de 24h) devolve esse id sem criar novo evento.
/// </summary>
public sealed class IdempotencyRecord
{
    /// <summary>Valor do header <c>Idempotency-Key</c> enviado pelo cliente.</summary>
    public string Key { get; set; } = null!;

    /// <summary>Id do lançamento (aggregateId) criado na primeira gravação.</summary>
    public Guid EntryId { get; set; }

    public DateTime CreatedAt { get; set; }
}
