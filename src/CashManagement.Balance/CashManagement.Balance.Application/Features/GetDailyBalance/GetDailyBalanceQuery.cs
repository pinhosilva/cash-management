namespace CashManagement.Balance.Application.Features.GetDailyBalance;

/// <summary>Consulta do saldo consolidado de um dia (read model).</summary>
public sealed record GetDailyBalanceQuery(DateOnly Date);
