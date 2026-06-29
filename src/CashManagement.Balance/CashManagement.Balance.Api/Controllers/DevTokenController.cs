using CashManagement.Balance.Api.Auth;
using CashManagement.Balance.Api.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CashManagement.Balance.Api.Controllers;

/// <summary>
/// Endpoint utilitário de desenvolvimento (§9.1): emite um JWT válido com o scope
/// <c>balances:read</c> para testar o <c>GET /balances/{date}</c> sem um IdP. Pode ser
/// ligado/desligado pela flag <c>Features:DevTokenEndpoint</c>, mas é <b>sempre</b>
/// <c>404</c> em produção (lá a emissão é do IdP real, §8.1) — a flag nunca o liga em prod.
/// </summary>
[ApiController]
[Route("dev/token")]
public sealed class DevTokenController : ControllerBase
{
    private readonly DevTokenService _tokens;
    private readonly IHostEnvironment _environment;
    private readonly FeatureFlags _features;

    public DevTokenController(DevTokenService tokens, IHostEnvironment environment, IOptions<FeatureFlags> features)
    {
        _tokens = tokens;
        _environment = environment;
        _features = features.Value;
    }

    /// <summary>Devolve um token de acesso com o scope de leitura (fora de produção e se a flag permitir).</summary>
    [HttpGet]
    public IActionResult Issue([FromQuery] string? subject)
    {
        // Trava dura de produção + flag (default ligado fora de produção).
        var enabled = !_environment.IsProduction() && (_features.DevTokenEndpoint ?? true);
        if (!enabled)
        {
            return NotFound();
        }

        var token = _tokens.IssueReadToken(string.IsNullOrWhiteSpace(subject) ? "dev-merchant" : subject);
        return Ok(new { access_token = token, token_type = "Bearer" });
    }
}
