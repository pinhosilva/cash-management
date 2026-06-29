namespace CashManagement.Entries.Api.Auth;

/// <summary>Nomes das políticas de autorização registradas no <c>Program</c>.</summary>
public static class AuthPolicies
{
    /// <summary>Exige o scope <c>entries:write</c> para registrar lançamentos (§8.1).</summary>
    public const string EntriesWrite = "entries:write";
}
