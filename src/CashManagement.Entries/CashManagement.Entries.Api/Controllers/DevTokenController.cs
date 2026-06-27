using CashManagement.Entries.Api.Auth;
using Microsoft.AspNetCore.Mvc;

namespace CashManagement.Entries.Api.Controllers;

/// <summary>
/// Endpoint utilitário de desenvolvimento (§9.1): emite um JWT válido com o scope
/// <c>entries:write</c> para testar o <c>POST /entries</c> sem um IdP. <b>Indisponível
/// em produção</b> — responde <c>404</c> (lá a emissão é responsabilidade do IdP real, §8.1),
/// para que a mesma chave de validação nunca emita tokens de escrita para anônimos.
/// </summary>
[ApiController]
[Route("dev/token")]
public sealed class DevTokenController : ControllerBase
{
    private readonly DevTokenService _tokens;
    private readonly IHostEnvironment _environment;

    public DevTokenController(DevTokenService tokens, IHostEnvironment environment)
    {
        _tokens = tokens;
        _environment = environment;
    }

    /// <summary>Devolve um token de acesso com o scope de escrita (apenas fora de produção).</summary>
    [HttpGet]
    public IActionResult Issue([FromQuery] string? subject)
    {
        if (_environment.IsProduction())
        {
            return NotFound();
        }

        var token = _tokens.IssueWriteToken(string.IsNullOrWhiteSpace(subject) ? "dev-merchant" : subject);
        return Ok(new { access_token = token, token_type = "Bearer" });
    }
}
