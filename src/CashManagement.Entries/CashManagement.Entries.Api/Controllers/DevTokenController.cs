using CashManagement.Entries.Api.Auth;
using Microsoft.AspNetCore.Mvc;

namespace CashManagement.Entries.Api.Controllers;

/// <summary>
/// Endpoint utilitário de desenvolvimento (§9.1): emite um JWT válido com o scope
/// <c>entries:write</c> para testar o <c>POST /entries</c> sem um IdP. Habilitado
/// apenas fora de produção (registrado no <c>Program</c> só quando não-Production).
/// </summary>
[ApiController]
[Route("dev/token")]
public sealed class DevTokenController : ControllerBase
{
    private readonly DevTokenService _tokens;

    public DevTokenController(DevTokenService tokens) => _tokens = tokens;

    /// <summary>Devolve um token de acesso com o scope de escrita.</summary>
    [HttpGet]
    public IActionResult Issue([FromQuery] string? subject)
    {
        var token = _tokens.IssueWriteToken(string.IsNullOrWhiteSpace(subject) ? "dev-merchant" : subject);
        return Ok(new { access_token = token, token_type = "Bearer" });
    }
}
