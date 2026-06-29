namespace CashManagement.Entries.Domain.SeedWork;

/// <summary>
/// Classe do erro esperado, mapeada para o HTTP status na borda (§4.4).
/// </summary>
public enum ErrorType
{
    Validation,
    Conflict,
    NotFound,
    Unauthorized,
    Forbidden
}

/// <summary>
/// Falha esperada de uma operação. Carrega um <see cref="Code"/> estável,
/// uma <see cref="Message"/> em inglês (linguagem ubíqua) e o
/// <see cref="ErrorType"/> que define o HTTP status.
/// </summary>
public sealed record Error(string Code, string Message, ErrorType Type);
