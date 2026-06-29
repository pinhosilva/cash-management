namespace CashManagement.Entries.Domain.Persistence;

/// <summary>
/// Fronteira de transação: persiste atomicamente tudo que foi encenado na
/// unidade de trabalho (eventos + outbox). Conflito de concorrência otimista
/// vira <c>ConcurrencyConflictException</c>. Acionado uma vez por comando, fora
/// do repositório (behavior do dispatcher).
/// </summary>
public interface IUnitOfWork
{
    Task CommitAsync(CancellationToken cancellationToken = default);
}
