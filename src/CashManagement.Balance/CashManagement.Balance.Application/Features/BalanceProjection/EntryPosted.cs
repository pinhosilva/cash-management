namespace CashManagement.Balance.Application.Features.BalanceProjection;

/// <summary>
/// Dados de um lançamento (<c>CreditPostedEvent</c>/<c>DebitPostedEvent</c>) de que a
/// projeção precisa, já extraídos do envelope (§4.3) pela camada de mensageria. Modelo
/// de leitura próprio do Balance — os serviços são desacoplados (§4.2), o Balance não
/// referencia o projeto Entries.
/// </summary>
/// <param name="EventId">Chave de dedup (<c>event.id</c>).</param>
/// <param name="Kind">Sentido do lançamento (crédito soma; débito subtrai).</param>
/// <param name="Amount">Valor do lançamento (<c>data.amount.amount</c>).</param>
/// <param name="OccurredAt">Instante do lançamento em UTC; o dia do saldo é <c>OccurredAt.Date</c>.</param>
/// <param name="CorrelationId">Rastreio propagado do evento (pode ser nulo).</param>
public sealed record EntryPosted(string EventId, EntryKind Kind, decimal Amount, DateTime OccurredAt, string? CorrelationId);
