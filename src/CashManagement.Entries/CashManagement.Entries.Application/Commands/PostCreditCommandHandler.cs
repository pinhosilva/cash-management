using CashManagement.Entries.Application.Abstractions;
using CashManagement.Entries.Application.Interfaces;
using CashManagement.Entries.Application.Validators;
using CashManagement.Entries.Domain.Aggregates;
using CashManagement.Entries.Domain.Repositories;
using CashManagement.Entries.Domain.SeedWork;
using CashManagement.Entries.Domain.ValueObjects;

namespace CashManagement.Entries.Application.Commands;

/// <summary>
/// Orquestra o registro de um crédito: valida → gera id → cria o agregado →
/// persiste (eventos + outbox, na implementação do repositório). A regra de
/// negócio vive no agregado; aqui é só orquestração. Não conhece SQL/Kafka.
/// </summary>
public sealed class PostCreditCommandHandler : ICommandHandler<PostCreditCommand, Guid>
{
    private readonly IEntryRepository _repository;
    private readonly IIdGenerator _ids;
    private readonly PostCreditCommandValidator _validator;

    public PostCreditCommandHandler(
        IEntryRepository repository,
        IIdGenerator ids,
        PostCreditCommandValidator validator)
    {
        _repository = repository;
        _ids = ids;
        _validator = validator;
    }

    public async Task<Result<Guid>> HandleAsync(PostCreditCommand command)
    {
        var validation = _validator.Validate(command);
        if (validation.IsFailure)
        {
            return Result.Fail<Guid>(validation.Error!);
        }

        var id = _ids.New();
        var entry = Entry.PostCredit(id, Money.Of(command.Amount, "BRL"), command.OccurredAt);
        await _repository.SaveAsync(entry);

        return Result.Ok(id);
    }
}
