namespace Domain.Results;

public enum ErrorType
{
    Unexpected,
    Validation,
    NotFound,
    Unauthorized,
    Conflict,
}

public record struct Error(ErrorType Type, string Message);

public class Result
{
    public bool IsSuccess => Errors.Count == 0; 
    public bool IsFailure => !IsSuccess;
    public IReadOnlyList<Error> Errors { get; }

    private Result(IReadOnlyList<Error> errors)
    {
        Errors = errors;
    }

    public static Result Ok() => new(Array.Empty<Error>());

    public static Result Fail(Error error) => new( new List<Error> { error });

    public static Result Fail(IReadOnlyList<Error> errors) => new(errors);

    public static Result<T> Ok<T>(T value)
    {
        return Result<T>.Ok(value);
    }

    public static Result<T> Fail<T>(Error error)
    {
        return Result<T>.Fail(error);
    }

    public static Result<T> Fail<T>(IReadOnlyList<Error> errors)
    {
        return Result<T>.Fail(errors);
    }
}

public class Result<T>
{
    public bool IsSuccess => Errors.Count == 0;
    public bool IsFailure => !IsSuccess;
    public T Value { get; }
    public IReadOnlyList<Error> Errors { get; }

    private Result(T value, IReadOnlyList<Error> errors)
    {
        Value = value;
        Errors = errors;
    }

    public static Result<T> Ok(T value) => new(value, Array.Empty<Error>());

    public static Result<T> Fail(Error error) => new(default!, new List<Error> { error });

    public static Result<T> Fail(IReadOnlyList<Error> errors) => new(default!, errors);
    
    public TResult Map<TResult>(Func<T, TResult> mapFunc)
    {
        if (IsFailure)
        {
            throw new InvalidOperationException("Cannot map a failed result.");
        }

        try
        {
            return mapFunc(Value);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Mapping failed: {ex.Message}");
        }
    }
}