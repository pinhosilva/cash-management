namespace CashManagement.Balance.Api.Auth;

/// <summary>Nomes das políticas de autorização registradas no <c>Program</c>.</summary>
public static class AuthPolicies
{
    /// <summary>Exige o scope <c>balances:read</c> para consultar o saldo (§8.1).</summary>
    public const string BalancesRead = "balances:read";
}
