using CashManagement.Entries.Domain.SeedWork;

namespace CashManagement.Entries.Application.Abstractions;

/// <summary>
/// Dispatcher próprio (Mediator enxuto). Resolve o
/// <see cref="ICommandHandler{TCommand,TResult}"/> pelo <see cref="IServiceProvider"/>
/// — tipo fechado conhecido em compilação, sem reflection. Handler não
/// registrado é erro de configuração: falha rápida (lança), não é Result.
/// </summary>
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
