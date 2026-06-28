namespace CashManagement.Balance.Api.Auth;

/// <summary>
/// Configuração do JWT de desenvolvimento (§9.1). A <see cref="SigningKey"/> é
/// uma chave estática <b>vinda de variável de ambiente</b> (nunca commitada);
/// um default só é aplicado em Development/testes. Em produção, a validação
/// aponta para o IdP real — sem mudar o código de validação.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>Scope exigido para o <c>GET /balances/{date}</c> (§8.1).</summary>
    public const string ReadScope = "balances:read";

    public string Issuer { get; set; } = "cash-management";

    public string Audience { get; set; } = "cash-management-balance";

    public string SigningKey { get; set; } = string.Empty;
}
