namespace Domain.Results;

public class Result<T>
{
    public bool IsSuccess => ErrorMessage == null;
    public T Value { get; private set; }
    public string? ErrorMessage { get; private set; }

    private Result(T value, string? errorMessage = null)
    {
        Value = value;
        ErrorMessage = errorMessage;
    }

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(string errorMessage) => new(default!, errorMessage);
}

public class Result
{
    public bool IsSuccess => ErrorMessage == null;
    public string? ErrorMessage { get; private set; }

    private Result(string? errorMessage = null)
    {
        ErrorMessage = errorMessage;
    }

    public static Result Success() => new();
    public static Result Failure(string errorMessage) => new(errorMessage);
}
