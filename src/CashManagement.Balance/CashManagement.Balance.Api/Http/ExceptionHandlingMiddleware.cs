using System.Text.Json;
using CashManagement.Balance.Api.Contracts;
using CashManagement.Balance.Api.Correlation;
using Microsoft.Extensions.Options;

namespace CashManagement.Balance.Api.Http;

/// <summary>
/// Middleware global de exceção (§4.4/§8.1): qualquer exceção inesperada vira
/// <c>500</c> no mesmo envelope, ecoando o <c>correlationId</c> e <b>sem vazar
/// stack trace</b>. Réplica adaptada do Entries (o Balance é só leitura, então não
/// há corrida de concorrência a traduzir).
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
