namespace CashManagement.Entries.Domain.ValueObjects;

/// <summary>
/// Value Object que classifica um lançamento. Conjunto fechado de valores
/// (instâncias estáticas), imutável e com igualdade por valor (record).
/// </summary>
/// <remarks>
/// Nesta fatia só <see cref="Credit"/> é usado (escopo: apenas crédito). O
/// <c>Debit</c> entra na fatia de débito, junto com seu comportamento.
/// </remarks>
public sealed record EntryType
{
    public static readonly EntryType Credit = new("Credit");

    private EntryType(string name) => Name = name;

    public string Name { get; }

    public override string ToString() => Name;
}
