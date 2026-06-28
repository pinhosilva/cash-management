using CashManagement.Entries.Api.Contracts;
using CashManagement.Entries.Domain.SeedWork;
using Microsoft.AspNetCore.Mvc;

namespace CashManagement.Entries.Api.Http;

/// <summary>
/// Tradutor único <c>Result</c> → envelope HTTP (§4.4). Concentra o mapa
/// <see cref="ErrorType"/> → HTTP status num só lugar, para o controller ficar
/// fino e o contrato de erro consistente.
/// </summary>
public static class ResultMapping
{
    /// <summary>Mapeia uma falha (<see cref="Error"/>) para o status HTTP da §4.4.</summary>
    public static int ToStatusCode(ErrorType type) => type switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status500InternalServerError,
    };

    /// <summary>Monta o <see cref="ObjectResult"/> de erro no envelope padronizado.</summary>
    public static ObjectResult ToErrorResult(Error error, string correlationId)
    {
        var body = ApiResponse<object>.Failure(
            new ApiError(error.Code, error.Message, []),
            correlationId);

        return new ObjectResult(body) { StatusCode = ToStatusCode(error.Type) };
    }
}
