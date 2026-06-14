namespace EventosVivos.Domain;

public enum ErrorType
{
    Validation,
    NotFound,
    Conflict,
    Unexpected
}

/// <summary>
/// Simple domain result type for business rule validation.
/// </summary>
public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string? Error { get; }
    public ErrorType ErrorType { get; }

    protected Result(bool isSuccess, string? error, ErrorType errorType = ErrorType.Unexpected)
    {
        IsSuccess = isSuccess;
        Error = error;
        ErrorType = errorType;
    }

    public static Result Success() => new(true, null);
    public static Result Failure(string error, ErrorType errorType = ErrorType.Conflict) => new(false, error, errorType);
}
