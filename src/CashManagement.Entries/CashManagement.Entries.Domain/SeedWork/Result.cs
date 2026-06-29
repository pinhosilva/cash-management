namespace CashManagement.Entries.Domain.SeedWork;

/// <summary>
/// Resultado de uma operação sem usar exceção como controle de fluxo (§5.10).
/// Sucesso ou falha; uma falha sempre carrega um <see cref="Error"/>.
/// </summary>
public class Result
{
    protected Result(bool isSuccess, Error? error)
    {
        if (isSuccess && error is not null)
        {
            throw new InvalidOperationException("A successful result cannot carry an error.");
        }

        if (!isSuccess && error is null)
        {
            throw new InvalidOperationException("A failed result must carry an error.");
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public Error? Error { get; }

    public static Result Ok() => new(true, null);

    public static Result Fail(Error error) => new(false, error);

    public static Result<T> Ok<T>(T value) => new(value, true, null);

    public static Result<T> Fail<T>(Error error) => new(default, false, error);
}

/// <summary>
/// <see cref="Result"/> que, no sucesso, carrega um valor (ex.: o id do stream
/// criado). Acessar <see cref="Value"/> de uma falha lança — é erro de uso.
/// </summary>
public sealed class Result<T> : Result
{
    private readonly T? _value;

    internal Result(T? value, bool isSuccess, Error? error) : base(isSuccess, error) => _value = value;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access the value of a failed result.");
}
