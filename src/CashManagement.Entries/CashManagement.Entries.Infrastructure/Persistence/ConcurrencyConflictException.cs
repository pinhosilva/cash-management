namespace CashManagement.Entries.Infrastructure.Persistence;

/// <summary>
/// Conflito de concorrência otimista ao commitar (versão de stream já gravada).
/// É uma corrida de infraestrutura, não falha de negócio — por isso é exceção
/// (mapeada para HTTP 409 na borda, §4.4), não <c>Result</c>.
/// </summary>
public sealed class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException(Exception innerException)
        : base("Concurrency conflict: a stream version was already written.", innerException)
    {
    }
}
