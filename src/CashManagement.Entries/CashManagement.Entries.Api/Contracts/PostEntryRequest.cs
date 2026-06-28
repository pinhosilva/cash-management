namespace CashManagement.Entries.Api.Contracts;

/// <summary>Corpo do <c>POST /entries</c> para registrar um crédito (§4.4).</summary>
public sealed record PostEntryRequest(decimal Amount, DateTime? OccurredAt);
