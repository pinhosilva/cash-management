using CashManagement.Entries.Domain.SeedWork;

namespace CashManagement.Entries.Application.Abstractions;

/// <summary>
/// Trata um <typeparamref name="TCommand"/> e devolve um
/// <see cref="Result{TResult}"/> (sucesso/falha, sem exceção como controle de fluxo).
/// </summary>
public interface ICommandHandler<TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    Task<Result<TResult>> HandleAsync(TCommand command);
}
