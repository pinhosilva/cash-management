namespace CashManagement.Balance.Application.Features.BalanceProjection;

/// <summary>
/// Porta da projeção do saldo diário (read model). Implementada na Infrastructure
/// (Mongo). A dedup por <c>event.id</c> e o ajuste do saldo acontecem de forma
/// atômica no mesmo documento — reaplicar a mesma <c>event.id</c> é no-op.
/// </summary>
public interface IDailyBalanceProjection
{
    /// <summary>
    /// Aplica um crédito à projeção do dia, de forma idempotente por
    /// <paramref name="eventId"/>. Se a <paramref name="eventId"/> já foi aplicada,
    /// não altera nada (no-op).
    /// </summary>
    /// <returns><c>true</c> se o crédito foi aplicado; <c>false</c> se já tinha sido (duplicado).</returns>
    Task<bool> ApplyCreditAsync(string eventId, DateOnly date, decimal amount, CancellationToken cancellationToken = default);
}
