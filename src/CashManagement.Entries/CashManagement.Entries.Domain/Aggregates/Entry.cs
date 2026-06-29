using CashManagement.Entries.Domain.Events;
using CashManagement.Entries.Domain.SeedWork;
using CashManagement.Entries.Domain.ValueObjects;

namespace CashManagement.Entries.Domain.Aggregates;

/// <summary>
/// Aggregate Root event-sourced de um lançamento. Registra crédito e débito: cada
/// factory assume entrada válida (a validação de valor positivo é da Application) e
/// <c>Emit</c> o evento correspondente; só o evento muta o estado, via
/// <c>On&lt;CreditPostedEvent&gt;</c> / <c>On&lt;DebitPostedEvent&gt;</c>.
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

    /// <summary>Registra um débito, emitindo o <see cref="DebitPostedEvent"/>.</summary>
    public static Entry PostDebit(Guid id, Money amount, DateTime occurredAt)
    {
        var entry = new Entry();
        entry.Emit(new DebitPostedEvent(id, amount, occurredAt));
        return entry;
    }

    protected override void RegisterEvents()
    {
        On<CreditPostedEvent>(e =>
        {
            Type = EntryType.Credit;
            Amount = e.Amount;
        });

        On<DebitPostedEvent>(e =>
        {
            Type = EntryType.Debit;
            Amount = e.Amount;
        });
    }
}
