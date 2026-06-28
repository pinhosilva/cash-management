namespace CashManagement.Balance.Api.Contracts;

/// <summary>
/// Recurso do <c>result</c> de sucesso do <c>GET /balances/{date}</c> (§4.4):
/// <c>{ date, totalCredits, totalDebits, balance }</c>. A <c>Date</c> é serializada
/// como <c>yyyy-MM-dd</c> (System.Text.Json para <see cref="DateOnly"/>).
/// </summary>
public sealed record DailyBalanceResponse(DateOnly Date, decimal TotalCredits, decimal TotalDebits, decimal Balance);
