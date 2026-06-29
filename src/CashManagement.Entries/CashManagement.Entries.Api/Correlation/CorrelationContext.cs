using CashManagement.Entries.Application.Interfaces;

namespace CashManagement.Entries.Api.Correlation;

/// <summary>
/// Implementação escopada do <see cref="ICorrelationContext"/>. Preenchida uma
/// vez por requisição pelo <see cref="CorrelationMiddleware"/> e lida onde o
/// envelope da outbox é montado (§4.3).
/// </summary>
public sealed class CorrelationContext : ICorrelationContext
{
    public string CorrelationId { get; private set; } = string.Empty;

    public string? InitiatedBy { get; private set; }

    public void Set(string correlationId, string? initiatedBy)
    {
        CorrelationId = correlationId;
        InitiatedBy = initiatedBy;
    }
}
