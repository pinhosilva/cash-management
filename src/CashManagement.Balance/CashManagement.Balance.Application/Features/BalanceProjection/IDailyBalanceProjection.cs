namespace CashManagement.Balance.Application.Features.BalanceProjection;

/// <summary>
/// Porta da projeção do saldo diário (read model). Implementada na Infrastructure
/// (Mongo). A dedup por <c>event.id</c> e o ajuste do saldo acontecem de forma
/// atômica no mesmo documento — reaplicar a mesma <c>event.id</c> é no-op.
/// </summary>
public interface IDailyBalanceProjection
{
    /// <summary>
    /// Aplica um lançamento à projeção do dia, de forma idempotente por
    /// <paramref name="eventId"/>. Crédito soma e débito subtrai do saldo, conforme
    /// <paramref name="kind"/>. Se a <paramref name="eventId"/> já foi aplicada, não
    /// altera nada (no-op).
    /// </summary>
    /// <returns><c>true</c> se foi aplicado; <c>false</c> se já tinha sido (duplicado).</returns>
    Task<bool> ApplyAsync(string eventId, DateOnly date, decimal amount, EntryKind kind, CancellationToken cancellationToken = default);
}
