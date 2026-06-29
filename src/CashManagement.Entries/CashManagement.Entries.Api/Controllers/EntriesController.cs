using CashManagement.Entries.Api.Auth;
using CashManagement.Entries.Api.Contracts;
using CashManagement.Entries.Api.Correlation;
using CashManagement.Entries.Api.Http;
using CashManagement.Entries.Application.Abstractions;
using CashManagement.Entries.Application.Features.PostCredit;
using CashManagement.Entries.Application.Features.PostDebit;
using CashManagement.Entries.Domain.Persistence;
using CashManagement.Entries.Domain.SeedWork;
using CashManagement.Entries.Domain.ValueObjects;
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

    /// <summary>
    /// Registra um lançamento. O corpo roteia por <c>type</c> (<c>Credit</c> default,
    /// <c>Debit</c>); o resto é idêntico: idempotência, commit do UoW e <c>201</c> com
    /// <c>Location</c> + envelope de sucesso (§4.4).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<EntryCreated>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> PostEntry([FromBody] PostEntryRequest request, CancellationToken cancellationToken)
    {
        var correlationId = _correlation.CorrelationId;

        if (!TryResolveType(request.Type, out var entryType))
        {
            _logger.LogInformation("Command rejected by validation. {component}", LogComponent.Controller);
            var error = new Error("VALIDATION_FAILED", "Type must be 'Credit' or 'Debit'.", ErrorType.Validation);
            return ResultMapping.ToErrorResult(error, correlationId);
        }

        var occurredAt = NormalizeToUtc(request.OccurredAt ?? DateTime.UtcNow);

        // Despacha o comando do tipo escolhido. Ambos devolvem o Guid do lançamento,
        // então o caminho de commit/envelope abaixo é o mesmo (default = Credit).
        var result = entryType == EntryType.Debit
            ? await _dispatcher.Send<PostDebitCommand, Guid>(new PostDebitCommand(request.Amount, occurredAt), cancellationToken)
            : await _dispatcher.Send<PostCreditCommand, Guid>(new PostCreditCommand(request.Amount, occurredAt), cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogInformation("Command rejected by validation. {component}", LogComponent.Controller);
            return ResultMapping.ToErrorResult(result.Error!, correlationId);
        }

        // Persiste evento + outbox + (quando há) chave de idempotência atomicamente.
        await _unitOfWork.CommitAsync(cancellationToken);

        var id = result.Value;
        _logger.LogInformation("Entry posted. {component} {entryType} {aggregateId}", LogComponent.Controller, entryType, id);

        return Created($"/entries/{id}",
            ApiResponse<EntryCreated>.Success(new EntryCreated(id.ToString()), correlationId));
    }

    /// <summary>
    /// Resolve o <c>type</c> do corpo: ausente/vazio ⇒ <see cref="EntryType.Credit"/>
    /// (retrocompatível); <c>"Credit"</c>/<c>"Debit"</c> (case-insensitive) ⇒ o tipo
    /// correspondente; qualquer outro valor ⇒ <c>false</c> (rejeição de contrato).
    /// </summary>
    private static bool TryResolveType(string? type, out EntryType entryType)
    {
        if (string.IsNullOrWhiteSpace(type) ||
            string.Equals(type, EntryType.Credit.Name, StringComparison.OrdinalIgnoreCase))
        {
            entryType = EntryType.Credit;
            return true;
        }

        if (string.Equals(type, EntryType.Debit.Name, StringComparison.OrdinalIgnoreCase))
        {
            entryType = EntryType.Debit;
            return true;
        }

        entryType = EntryType.Credit;
        return false;
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
