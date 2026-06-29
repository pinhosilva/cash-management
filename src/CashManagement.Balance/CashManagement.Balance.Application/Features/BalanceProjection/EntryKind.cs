namespace CashManagement.Balance.Application.Features.BalanceProjection;

/// <summary>
/// Sentido de um lançamento na projeção do saldo diário. <see cref="Credit"/> soma
/// (totalCredits, balance += amount); <see cref="Debit"/> subtrai (totalDebits,
/// balance -= amount). O <c>balance</c> resultante é <c>totalCredits - totalDebits</c>.
/// </summary>
public enum EntryKind
{
    Credit,
    Debit,
}
