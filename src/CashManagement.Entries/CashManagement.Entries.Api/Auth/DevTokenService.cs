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

    public string IssueWriteToken(string subject = "dev-merchant")
    {
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey)),
            SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, subject),
            new Claim("scope", JwtOptions.WriteScope),
        };

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
