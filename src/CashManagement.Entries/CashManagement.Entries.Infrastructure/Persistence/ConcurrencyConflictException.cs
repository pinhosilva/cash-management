namespace CashManagement.Entries.Infrastructure.Persistence;

/// <summary>
/// Conflito de concorrência otimista ao gravar um stream (versão esperada
/// divergente). É uma corrida de infraestrutura, não falha de negócio — por
/// isso é exceção (mapeada para HTTP 409 na borda, §4.4), não <c>Result</c>.
/// </summary>
public sealed class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException(Guid aggregateId, int expectedVersion, Exception innerException)
        : base($"Concurrency conflict on aggregate '{aggregateId}' at expected version {expectedVersion}.", innerException)
    {
        AggregateId = aggregateId;
        ExpectedVersion = expectedVersion;
    }

    public Guid AggregateId { get; }

    public int ExpectedVersion { get; }
}
