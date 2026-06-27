using CashManagement.Entries.Domain.Persistence;
using CashManagement.Entries.Domain.SeedWork;

namespace CashManagement.Entries.Application.Abstractions;

/// <summary>
/// Dispatcher próprio (Mediator enxuto). Resolve o
/// <see cref="ICommandHandler{TCommand,TResult}"/> pelo <see cref="IServiceProvider"/>
/// — tipo fechado conhecido em compilação, sem reflection. Handler não
/// registrado é erro de configuração: falha rápida (lança), não é Result.
/// </summary>
/// <remarks>
/// É o ponto único de cross-cutting (§5.10): aqui mora o behavior transacional —
/// se o handler tem sucesso, a unidade de trabalho é commitada uma única vez
/// (Unit of Work isolado do repositório).
/// </remarks>
public sealed class CommandDispatcher : ICommandDispatcher
{
    private readonly IServiceProvider _provider;
    private readonly IUnitOfWork _unitOfWork;

    public CommandDispatcher(IServiceProvider provider, IUnitOfWork unitOfWork)
    {
        _provider = provider;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<TResult>> Send<TCommand, TResult>(TCommand command)
        where TCommand : ICommand<TResult>
    {
        var handler = (ICommandHandler<TCommand, TResult>?)_provider.GetService(typeof(ICommandHandler<TCommand, TResult>))
            ?? throw new InvalidOperationException($"No handler registered for command '{typeof(TCommand).Name}'.");

        var result = await handler.HandleAsync(command);
        if (result.IsSuccess)
        {
            await _unitOfWork.CommitAsync();
        }

        return result;
    }
}
