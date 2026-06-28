namespace CashManagement.Balance.Api.Contracts;

/// <summary>
/// Envelope padronizado de resposta (§4.4) — mesma forma para sucesso e erro.
/// O <c>Status</c> sempre espelha o HTTP status; <c>CorrelationId</c> é ecoado
/// em toda resposta. Em sucesso vem <c>Result</c>; em falha vem <c>Error</c>.
/// Réplica adaptada do envelope do Entries (serviços separados, sem acoplamento).
/// </summary>
public sealed record ApiResponse<T>(string Status, T? Result, ApiError? Error, string CorrelationId)
{
    public static ApiResponse<T> Success(T result, string correlationId) =>
        new("success", result, null, correlationId);

    public static ApiResponse<T> Failure(ApiError error, string correlationId) =>
        new("error", default, error, correlationId);
}

/// <summary>Corpo de erro do envelope (§4.4): código estável, mensagem em inglês e detalhes.</summary>
public sealed record ApiError(string Code, string Message, IReadOnlyList<string> Details);
