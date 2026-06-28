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
/// Unit of Work, na fronteira do caso de uso). A regra vive no agregado; aqui é
/// só orquestração. Não conhece SQL/Kafka/transação.
/// </summary>
public sealed class PostCreditCommandHandler : ICommandHandler<PostCreditCommand, Guid>
{
    private readonly IRepository _repository;
    private readonly IIdGenerator _ids;
    private readonly PostCreditCommandValidator _validator;

    public PostCreditCommandHandler(IRepository repository, IIdGenerator ids, PostCreditCommandValidator validator)
    {
        _repository = repository;
        _ids = ids;
        _validator = validator;
    }

    public Task<Result<Guid>> HandleAsync(PostCreditCommand command, CancellationToken cancellationToken = default)
    {
        var validation = _validator.Validate(command);
        if (validation.IsFailure)
        {
            return Task.FromResult(Result.Fail<Guid>(validation.Error!));
        }

        var id = _ids.New();
        var entry = Entry.PostCredit(id, Money.Of(command.Amount, "BRL"), command.OccurredAt);
        _repository.Add(entry);

        return Task.FromResult(Result.Ok(id));
    }
}
