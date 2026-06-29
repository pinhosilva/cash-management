namespace CashManagement.Balance.Application.Features.GetDailyBalance;

/// <summary>
/// Projeção do saldo consolidado de um dia (read model, §4.4). É o recurso devolvido
/// no <c>result</c> do envelope de sucesso do <c>GET /balances/{date}</c>.
/// </summary>
public sealed record DailyBalanceDto(DateOnly Date, decimal TotalCredits, decimal TotalDebits, decimal Balance);
