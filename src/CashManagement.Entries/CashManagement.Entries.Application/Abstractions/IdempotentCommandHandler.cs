using CashManagement.Entries.Application.Interfaces;
using CashManagement.Entries.Domain.SeedWork;

namespace CashManagement.Entries.Application.Abstractions;

/// <summary>
/// Behavior de idempotência de escrita (§4.3/§5.10): <b>decora</b> um handler que cria
/// um agregado (retorna o <c>Guid</c> do lançamento). Se a <c>Idempotency-Key</c> da
/// requisição já foi processada na janela de dedup, devolve o id original sem tocar o
/// domínio; caso contrário executa o handler interno e registra a chave — o
/// <c>Add</c> só encena, persistido na mesma transação do evento pelo
/// <c>IUnitOfWork</c> na fronteira do caso de uso. Mantém handler e controller
/// alheios à deduplicação (sem lógica espalhada).
/// </summary>
public sealed class IdempotentCommandHandler<TCommand> : ICommandHandler<TCommand, Guid>
    where TCommand : ICommand<Guid>
{
    private readonly ICommandHandler<TCommand, Guid> _inner;
    private readonly IIdempotencyStore _store;
    private readonly IIdempotencyContext _context;

    public IdempotentCommandHandler(
        ICommandHandler<TCommand, Guid> inner,
        IIdempotencyStore store,
        IIdempotencyContext context)
    {
        _inner = inner;
        _store = store;
        _context = context;
    }

    public async Task<Result<Guid>> HandleAsync(TCommand command, CancellationToken cancellationToken = default)
    {
        var key = _context.Key;

        if (key is not null && await _store.FindAsync(key, cancellationToken) is { } existingId)
        {
            return Result.Ok(existingId);
        }

        var result = await _inner.HandleAsync(command, cancellationToken);
        if (result.IsSuccess && key is not null)
        {
            _store.Add(key, result.Value);
        }

        return result;
    }
}
