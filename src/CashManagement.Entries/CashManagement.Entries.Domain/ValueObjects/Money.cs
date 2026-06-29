namespace CashManagement.Entries.Domain.ValueObjects;

/// <summary>
/// Value Object monetário: quantia + moeda, imutável e com igualdade por valor
/// (record). Evita primitive obsession sobre um <see cref="decimal"/> solto.
/// </summary>
/// <remarks>
/// A regra de negócio "valor positivo" é validada como <c>Result</c>
/// (ErrorType.Validation) no validator da Application (T05), não aqui — o
/// <see cref="Money"/> não usa exceção como caminho de validação.
/// Nesta fatia a moeda é fixa em BRL (premissa de moeda única — §3).
/// </remarks>
public sealed record Money(decimal Amount, string Currency)
{
    public static Money Of(decimal amount, string currency) => new(amount, currency);
}
