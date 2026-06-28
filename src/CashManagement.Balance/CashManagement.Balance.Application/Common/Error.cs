namespace CashManagement.Balance.Application.Common;

/// <summary>
/// Classe do erro esperado, mapeada para o HTTP status na borda (§4.4).
/// Espelha o <c>ErrorType</c> do Entries; o Balance só usa um subconjunto
/// (consulta read-only), mas mantém o enum completo por consistência.
/// </summary>
public enum ErrorType
{
    Validation,
    Conflict,
    NotFound,
    Unauthorized,
    Forbidden,
}

/// <summary>
/// Falha esperada de uma operação. Carrega um <see cref="Code"/> estável,
/// uma <see cref="Message"/> em inglês (linguagem ubíqua) e o
/// <see cref="ErrorType"/> que define o HTTP status.
/// </summary>
public sealed record Error(string Code, string Message, ErrorType Type)
{
    /// <summary>Saldo do dia inexistente na projeção (nenhum lançamento naquele dia).</summary>
    public static Error NotFound(string message) => new("NOT_FOUND", message, ErrorType.NotFound);

    /// <summary>Entrada inválida (ex.: data em formato diferente de <c>yyyy-MM-dd</c>).</summary>
    public static Error Validation(string message) => new("VALIDATION_FAILED", message, ErrorType.Validation);
}
