using System.Globalization;
using CashManagement.Balance.Api.Auth;
using CashManagement.Balance.Api.Contracts;
using CashManagement.Balance.Api.Correlation;
using CashManagement.Balance.Api.Http;
using CashManagement.Balance.Application.Common;
using CashManagement.Balance.Application.Features.GetDailyBalance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CashManagement.Balance.Api.Controllers;

/// <summary>
/// API de consulta do saldo consolidado. Controller <b>fino</b>: parseia/valida a
/// data, monta a query, despacha ao handler da Application e traduz o <c>Result</c>
/// para o envelope (§4.4). Lê direto da projeção Mongo — nunca chama o Entries (§4.2).
/// O handler é injetado direto (sem dispatcher): a leitura é única e não tem behaviors.
/// </summary>
[ApiController]
[Route("balances")]
[Authorize(Policy = AuthPolicies.BalancesRead)]
public sealed class BalancesController : ControllerBase
{
    private readonly GetDailyBalanceQueryHandler _handler;
    private readonly CorrelationContext _correlation;
    private readonly ILogger<BalancesController> _logger;

    public BalancesController(
        GetDailyBalanceQueryHandler handler,
        CorrelationContext correlation,
        ILogger<BalancesController> logger)
    {
        _handler = handler;
        _correlation = correlation;
        _logger = logger;
    }

    /// <summary>Consulta o saldo consolidado do dia <paramref name="date"/> (formato <c>yyyy-MM-dd</c>).</summary>
    [HttpGet("{date}")]
    [ProducesResponseType(typeof(ApiResponse<DailyBalanceResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDailyBalance(string date, CancellationToken cancellationToken)
    {
        var correlationId = _correlation.CorrelationId;

        if (!DateOnly.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
        {
            _logger.LogInformation("Invalid date format on balance query. {component}", LogComponent.Controller);
            return ResultMapping.ToErrorResult(
                Error.Validation("Date must be in 'yyyy-MM-dd' format."), correlationId);
        }

        var result = await _handler.HandleAsync(new GetDailyBalanceQuery(parsedDate), cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogInformation("Balance not found for the requested date. {component}", LogComponent.Controller);
            return ResultMapping.ToErrorResult(result.Error!, correlationId);
        }

        var dto = result.Value;
        var response = new DailyBalanceResponse(dto.Date, dto.TotalCredits, dto.TotalDebits, dto.Balance);

        _logger.LogInformation("Balance served. {component}", LogComponent.Controller);

        return Ok(ApiResponse<DailyBalanceResponse>.Success(response, correlationId));
    }
}
