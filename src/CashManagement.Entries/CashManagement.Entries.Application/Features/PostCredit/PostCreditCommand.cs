using CashManagement.Entries.Application.Abstractions;

namespace CashManagement.Entries.Application.Features.PostCredit;

/// <summary>Intenção de registrar um crédito. Devolve o id do lançamento criado.</summary>
public sealed record PostCreditCommand(decimal Amount, DateTime OccurredAt) : ICommand<Guid>;
