namespace CashManagement.Entries.Api.Contracts;

/// <summary>
/// Corpo do <c>POST /entries</c> (§4.4). O <c>Type</c> roteia entre crédito e débito;
/// é <b>opcional</b> e, quando ausente, assume <c>Credit</c> — mantém retrocompatível
/// quem só manda <c>{ amount, occurredAt }</c>. Aceita <c>"Credit"</c>/<c>"Debit"</c>
/// (case-insensitive); qualquer outro valor é rejeitado na validação do contrato.
/// </summary>
public sealed record PostEntryRequest(decimal Amount, DateTime? OccurredAt, string? Type = null);
