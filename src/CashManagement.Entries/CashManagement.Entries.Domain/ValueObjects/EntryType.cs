namespace CashManagement.Entries.Domain.ValueObjects;

/// <summary>
/// Value Object que classifica um lançamento. Conjunto fechado de valores
/// (instâncias estáticas), imutável e com igualdade por valor (record).
/// </summary>
/// <remarks>
/// <see cref="Credit"/> soma ao saldo; <see cref="Debit"/> subtrai. Cada valor é
/// classificado pelo evento que reconstrói o agregado (ver <c>Entry.RegisterEvents</c>).
/// </remarks>
public sealed record EntryType
{
    public static readonly EntryType Credit = new("Credit");
    public static readonly EntryType Debit = new("Debit");

    private EntryType(string name) => Name = name;

    public string Name { get; }

    public override string ToString() => Name;
}
