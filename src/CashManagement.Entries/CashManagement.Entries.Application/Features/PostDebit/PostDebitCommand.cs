using CashManagement.Entries.Application.Abstractions;

namespace CashManagement.Entries.Application.Features.PostDebit;

/// <summary>Intenção de registrar um débito. Devolve o id do lançamento criado.</summary>
public sealed record PostDebitCommand(decimal Amount, DateTime OccurredAt) : ICommand<Guid>;
