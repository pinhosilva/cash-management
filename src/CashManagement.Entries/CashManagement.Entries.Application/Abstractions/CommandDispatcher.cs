using CashManagement.Entries.Domain.SeedWork;

namespace CashManagement.Entries.Application.Abstractions;

/// <summary>
/// Dispatcher próprio (Mediator enxuto) — <b>responsabilidade única</b>: resolver
/// o <see cref="ICommandHandler{TCommand,TResult}"/> e despachar. Resolve pelo
/// <see cref="IServiceProvider"/> (tipo fechado em compilação, sem reflection).
/// </summary>
/// <remarks>
/// NÃO controla transação. O commit é do <c>IUnitOfWork</c>, acionado na
/// fronteira do caso de uso (a request na API, ou um orquestrador quando vários
/// comandos formam um "pacotão" com um único commit) — fora do dispatcher.
/// </remarks>
public sealed class CommandDispatcher : ICommandDispatcher
{
    private readonly IServiceProvider _provider;

    public CommandDispatcher(IServiceProvider provider) => _provider = provider;

    public Task<Result<TResult>> Send<TCommand, TResult>(TCommand command)
        where TCommand : ICommand<TResult>
    {
        var handler = (ICommandHandler<TCommand, TResult>?)_provider
            .GetService(typeof(ICommandHandler<TCommand, TResult>))
            ?? throw new InvalidOperationException(
                $"No handler registered for command '{typeof(TCommand).Name}'.");

        return handler.HandleAsync(command);
    }
}
