using CashManagement.Entries.Domain.SeedWork;

namespace CashManagement.Entries.Application.Abstractions;

/// <summary>
/// Despacha um comando para o seu handler, devolvendo o <see cref="Result{TResult}"/>.
/// Genérico sobre o tipo concreto do comando (<typeparamref name="TCommand"/>) —
/// o handler é resolvido por DI sem reflection.
/// </summary>
public interface ICommandDispatcher
{
    Task<Result<TResult>> Send<TCommand, TResult>(TCommand command, CancellationToken cancellationToken = default) where TCommand : ICommand<TResult>;
}
