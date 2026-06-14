using EventosVivos.Domain;

namespace EventosVivos.Application;

/// <summary>
/// Generic result type that carries data on success or an error on failure.
/// </summary>
public class Result<T> : Domain.Result
{
    public T? Data { get; }

    protected Result(bool isSuccess, string? error, T? data, ErrorType errorType = ErrorType.Unexpected)
        : base(isSuccess, error, errorType)
    {
        Data = data;
    }

    public static Result<T> Success(T data) => new(true, null, data);
    public new static Result<T> Failure(string error, ErrorType errorType = ErrorType.Conflict) =>
        new(false, error, default, errorType);
}
