namespace CashManagement.Balance.Application.Features.BalanceProjection;

/// <summary>
/// Dados de um <c>CreditPostedEvent</c> de que a projeção precisa, já extraídos do
/// envelope (§4.3) pela camada de mensageria. Modelo de leitura próprio do Balance —
/// os serviços são desacoplados (§4.2), o Balance não referencia o projeto Entries.
/// </summary>
/// <param name="EventId">Chave de dedup (<c>event.id</c>).</param>
/// <param name="Amount">Valor do crédito (<c>data.amount.amount</c>).</param>
/// <param name="OccurredAt">Instante do crédito em UTC; o dia do saldo é <c>OccurredAt.Date</c>.</param>
/// <param name="CorrelationId">Rastreio propagado do evento (pode ser nulo).</param>
public sealed record CreditPosted(string EventId, decimal Amount, DateTime OccurredAt, string? CorrelationId);
