using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CashManagement.Entries.Api.Auth;

/// <summary>
/// Emissor de tokens de desenvolvimento (§9.1): gera um JWT válido (assinatura,
/// issuer, audience, exp) com o scope <c>entries:write</c>, para testar localmente
/// sem um IdP. A chave é a mesma chave estática de dev usada na validação.
/// </summary>
public sealed class DevTokenService
{
    private readonly JwtOptions _options;

    public DevTokenService(IOptions<JwtOptions> options) => _options = options.Value;

    /// <summary>Emite um token com o scope <c>entries:write</c> (caminho feliz do <c>POST /entries</c>).</summary>
    public string IssueWriteToken(string subject = "dev-merchant") =>
        IssueToken(subject, JwtOptions.WriteScope);

    /// <summary>
    /// Emite um JWT válido (assinatura/issuer/audience/exp) com os <paramref name="scopes"/>
    /// informados. Permite emitir um token autenticado <b>sem</b> o scope de escrita —
    /// usado para exercer o caminho de <c>403</c> (§8.1).
    /// </summary>
    public string IssueToken(string subject, params string[] scopes)
    {
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey)),
            SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim> { new(JwtRegisteredClaimNames.Sub, subject) };
        claims.AddRange(scopes.Select(scope => new Claim("scope", scope)));

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
