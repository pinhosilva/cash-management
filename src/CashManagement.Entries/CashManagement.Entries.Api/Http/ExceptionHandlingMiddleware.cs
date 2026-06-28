using System.Text.Json;
using CashManagement.Entries.Api.Contracts;
using CashManagement.Entries.Api.Correlation;
using CashManagement.Entries.Infrastructure.Persistence;
using Microsoft.Extensions.Options;

namespace CashManagement.Entries.Api.Http;

/// <summary>
/// Middleware global de exceção (§4.4/§8.1): a corrida de concorrência otimista
/// (<see cref="ConcurrencyConflictException"/>) vira <c>409</c>; qualquer outra
/// exceção inesperada vira <c>500</c> — sempre no mesmo envelope, ecoando o
/// <c>correlationId</c> e <b>sem vazar stack trace</b>.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly JsonSerializerOptions _json;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IOptions<Microsoft.AspNetCore.Mvc.JsonOptions> jsonOptions)
    {
        _next = next;
        _logger = logger;
        _json = jsonOptions.Value.JsonSerializerOptions;
    }

    public async Task InvokeAsync(HttpContext context, CorrelationContext correlation)
    {
        try
        {
            await _next(context);
        }
        catch (ConcurrencyConflictException)
        {
            _logger.LogWarning("Concurrency conflict while persisting an entry. {component}", LogComponent.Controller);
            await WriteAsync(context, StatusCodes.Status409Conflict,
                new ApiError("CONFLICT", "Concurrency conflict.", []), correlation.CorrelationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception while processing the request. {component}", LogComponent.Controller);
            await WriteAsync(context, StatusCodes.Status500InternalServerError,
                new ApiError("INTERNAL_ERROR", "An unexpected error occurred.", []), correlation.CorrelationId);
        }
    }

    private async Task WriteAsync(HttpContext context, int statusCode, ApiError error, string correlationId)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var body = ApiResponse<object>.Failure(error, correlationId);
        await context.Response.WriteAsync(JsonSerializer.Serialize(body, _json));
    }
}
