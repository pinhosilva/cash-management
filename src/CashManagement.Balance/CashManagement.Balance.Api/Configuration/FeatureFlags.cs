namespace CashManagement.Balance.Api.Configuration;

/// <summary>
/// Feature flags da API (seção <c>Features</c> do appsettings). Cada flag é
/// <b>anulável</b> com a semântica: <c>null</c> = segue o default do ambiente
/// (hoje, <c>!Production</c>); <c>true</c>/<c>false</c> = força. Espelha o Entries.
/// </summary>
public sealed class FeatureFlags
{
    public const string SectionName = "Features";

    /// <summary>Expõe o Swagger UI. <c>null</c> ⇒ habilitado fora de produção.</summary>
    public bool? Swagger { get; set; }

    /// <summary>Habilita o endpoint <c>GET /dev/token</c>. <c>null</c> ⇒ fora de produção; <b>nunca</b> liga em produção.</summary>
    public bool? DevTokenEndpoint { get; set; }
}
