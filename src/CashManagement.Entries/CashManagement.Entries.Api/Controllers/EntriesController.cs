using CashManagement.Entries.Api.Auth;
using CashManagement.Entries.Api.Contracts;
using CashManagement.Entries.Api.Correlation;
using CashManagement.Entries.Api.Http;
using CashManagement.Entries.Application.Abstractions;
using CashManagement.Entries.Application.Features.PostCredit;
using CashManagement.Entries.Domain.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CashManagement.Entries.Api.Controllers;

/// <summary>
/// API de Lançamentos. Controller <b>fino</b>: monta o comando, despacha para a
/// Application e traduz o <c>Result</c> para o envelope (§4.4). A deduplicação de
/// idempotência é um <i>behavior</i> em volta do <c>Send</c> (§5.10); o commit é do
/// <c>IUnitOfWork</c>, na fronteira do caso de uso. Sem lógica de negócio aqui.
/// </summary>
[ApiController]
[Route("entries")]
[Authorize(Policy = AuthPolicies.EntriesWrite)]
public sealed class EntriesController : ControllerBase
{
    private readonly ICommandDispatcher _dispatcher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly CorrelationContext _correlation;
    private readonly ILogger<EntriesController> _logger;

    public EntriesController(
        ICommandDispatcher dispatcher,
        IUnitOfWork unitOfWork,
        CorrelationContext correlation,
        ILogger<EntriesController> logger)
    {
        _dispatcher = dispatcher;
        _unitOfWork = unitOfWork;
        _correlation = correlation;
        _logger = logger;
    }

    /// <summary>Registra um crédito. Retorna <c>201</c> com <c>Location</c> e o envelope de sucesso.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<EntryCreated>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> PostCredit([FromBody] PostEntryRequest request, CancellationToken cancellationToken)
    {
        var correlationId = _correlation.CorrelationId;

        var occurredAt = NormalizeToUtc(request.OccurredAt ?? DateTime.UtcNow);
        var command = new PostCreditCommand(request.Amount, occurredAt);
        var result = await _dispatcher.Send<PostCreditCommand, Guid>(command, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogInformation("Command rejected by validation. {component}", LogComponent.Controller);
            return ResultMapping.ToErrorResult(result.Error!, correlationId);
        }

        // Persiste evento + outbox + (quando há) chave de idempotência atomicamente.
        await _unitOfWork.CommitAsync(cancellationToken);

        var id = result.Value;
        _logger.LogInformation("Credit posted. {component} {aggregateId}", LogComponent.Controller, id);

        return Created($"/entries/{id}",
            ApiResponse<EntryCreated>.Success(new EntryCreated(id.ToString()), correlationId));
    }

    /// <summary>
    /// Garante que o <c>OccurredAt</c> entre no event store imutável sempre em UTC
    /// (§4.3): eventos não são reparáveis depois. Valor já-UTC passa direto; Local é
    /// convertido; Unspecified é assumido como UTC (a API documenta entrada em UTC).
    /// </summary>
    private static DateTime NormalizeToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
    };
}
