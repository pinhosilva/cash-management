namespace CashManagement.Balance.Api.Correlation;

/// <summary>
/// Contexto de correlação escopado da requisição. Preenchido uma vez pelo
/// <see cref="CorrelationMiddleware"/> e lido pelo controller para ecoar o
/// <c>correlationId</c> no envelope (§4.3). Réplica enxuta do Entries (sem
/// idempotência: o Balance é só leitura).
/// </summary>
public sealed class CorrelationContext
{
    public string CorrelationId { get; private set; } = string.Empty;

    public void Set(string correlationId) => CorrelationId = correlationId;
}
