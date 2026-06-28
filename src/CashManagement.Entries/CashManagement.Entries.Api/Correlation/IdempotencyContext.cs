using CashManagement.Entries.Application.Interfaces;

namespace CashManagement.Entries.Api.Correlation;

/// <summary>
/// Implementação escopada do <see cref="IIdempotencyContext"/>. Preenchida uma vez
/// por requisição pelo <see cref="CorrelationMiddleware"/> (a borda que lê os
/// metadados da request) a partir do header <c>Idempotency-Key</c> (§4.3).
/// </summary>
public sealed class IdempotencyContext : IIdempotencyContext
{
    public string? Key { get; private set; }

    public void Set(string? key) => Key = key;
}
