using CashManagement.Entries.Application.Abstractions;
using CashManagement.Entries.Application.Interfaces;
using CashManagement.Entries.Domain.Aggregates;
using CashManagement.Entries.Domain.Persistence;
using CashManagement.Entries.Domain.SeedWork;
using CashManagement.Entries.Domain.ValueObjects;

namespace CashManagement.Entries.Application.Features.PostCredit;

/// <summary>
/// Orquestra o registro de um crédito: valida → gera id → cria o agregado →
/// <b>encena</b> os eventos no event store (sem commit — o commit é do
/// Unit of Work, no behavior do dispatcher). A regra vive no agregado; aqui é
/// só orquestração. Não conhece SQL/Kafka/transação.
/// </summary>
public sealed class PostCreditCommandHandler : ICommandHandler<PostCreditCommand, Guid>
{
    private readonly IEventStore _eventStore;
    private readonly IIdGenerator _ids;
    private readonly PostCreditCommandValidator _validator;

    public PostCreditCommandHandler(
        IEventStore eventStore,
        IIdGenerator ids,
        PostCreditCommandValidator validator)
    {
        _eventStore = eventStore;
        _ids = ids;
        _validator = validator;
    }

    public Task<Result<Guid>> HandleAsync(PostCreditCommand command)
    {
        var validation = _validator.Validate(command);
        if (validation.IsFailure)
        {
            return Task.FromResult(Result.Fail<Guid>(validation.Error!));
        }

        var id = _ids.New();
        var entry = Entry.PostCredit(id, Money.Of(command.Amount, "BRL"), command.OccurredAt);
        _eventStore.Append(entry);

        return Task.FromResult(Result.Ok(id));
    }
}
