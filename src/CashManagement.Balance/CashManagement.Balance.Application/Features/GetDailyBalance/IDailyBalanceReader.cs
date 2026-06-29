namespace CashManagement.Balance.Application.Features.GetDailyBalance;

/// <summary>
/// Porta de <b>leitura</b> da projeção do saldo diário. Definida na Application e
/// implementada na Infrastructure (Mongo). É só leitura: serve a consulta
/// <c>GET /balances/{date}</c> direto do read model, sem nunca chamar o Entries (§4.2).
/// </summary>
public interface IDailyBalanceReader
{
    /// <summary>
    /// Lê o saldo consolidado do <paramref name="date"/> a partir da projeção.
    /// </summary>
    /// <returns>O <see cref="DailyBalanceDto"/> do dia, ou <c>null</c> se não houver projeção (nenhum lançamento).</returns>
    Task<DailyBalanceDto?> GetAsync(DateOnly date, CancellationToken cancellationToken = default);
}
