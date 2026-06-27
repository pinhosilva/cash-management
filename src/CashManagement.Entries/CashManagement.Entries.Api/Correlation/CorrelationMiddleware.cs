using System.Security.Claims;
using Serilog.Context;

namespace CashManagement.Entries.Api.Correlation;

/// <summary>
/// Borda da rastreabilidade (§4.3): lê o header <c>X-Correlation-Id</c> (gera um
/// UUID se ausente), preenche o <see cref="CorrelationContext"/> escopado, ecoa o
/// valor no header de resposta e o empurra para o <c>LogContext</c> do Serilog —
/// de modo que toda linha de log da requisição carregue o mesmo correlationId.
/// O <c>initiatedBy</c> vem da identidade do JWT (quando autenticado).
/// </summary>
public sealed class CorrelationMiddleware
{
    public const string HeaderName = "X-Correlation-Id";

    private readonly RequestDelegate _next;

    public CorrelationMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, CorrelationContext correlation)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var header)
            && !string.IsNullOrWhiteSpace(header)
                ? header.ToString()
                : Guid.NewGuid().ToString();

        var initiatedBy = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.User.FindFirstValue("sub");

        correlation.Set(correlationId, initiatedBy);
        context.Response.Headers[HeaderName] = correlationId;

        using (LogContext.PushProperty("correlationId", correlationId))
        {
            await _next(context);
        }
    }
}
