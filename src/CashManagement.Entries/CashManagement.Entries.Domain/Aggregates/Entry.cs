using CashManagement.Entries.Domain.Events;
using CashManagement.Entries.Domain.SeedWork;
using CashManagement.Entries.Domain.ValueObjects;

namespace CashManagement.Entries.Domain.Aggregates;

/// <summary>
/// Aggregate Root event-sourced de um lançamento. Nesta fatia só registra
/// crédito: a factory valida (assume entrada válida — a validação de valor
/// positivo é da Application, T05) e <c>Emit</c> o evento; só o evento muta o
/// estado, via <c>On&lt;CreditPostedEvent&gt;</c>.
/// </summary>
public sealed class Entry : AggregateRoot
{
    private Entry() {  }

    public EntryType Type { get; private set; } = null!;
    public Money Amount { get; private set; } = null!;

    /// <summary>Registra um crédito, emitindo o <see cref="CreditPostedEvent"/>.</summary>
    public static Entry PostCredit(Guid id, Money amount, DateTime occurredAt)
    {
        var entry = new Entry();
        entry.Emit(new CreditPostedEvent(id, amount, occurredAt));
        return entry;
    }

    protected override void RegisterEvents() =>
        On<CreditPostedEvent>(e =>
        {
            Type = EntryType.Credit;
            Amount = e.Amount;
        });
}
