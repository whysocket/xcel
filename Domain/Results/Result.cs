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

    public Result(IReadOnlyList<Error> errors)
    {
        Errors = errors;
    }

    public static Result Ok() => new(Array.Empty<Error>());

    public static Result Fail(Error error) => new( new List<Error> { error });

    public static Result Fail(IReadOnlyList<Error> errors) => new(errors);

    // New generic Ok method for type inference
    public static Result<T> Ok<T>(T value)
    {
        return Result<T>.Ok(value);
    }

    // New generic Fail method for type inference
    public static Result<T> Fail<T>(Error error)
    {
        return Result<T>.Fail(error);
    }

    public static Result<T> Fail<T>(IReadOnlyList<Error> errors)
    {
        return Result<T>.Fail(errors);
    }

    public static implicit operator Result(Result<object> result)
    {
        return new Result(result.Errors);
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

    public static implicit operator Result(Result<T> result)
    {
        return new Result(result.Errors);
    }
    
    // Modified Map method to accept a Func<T, TResult> and returning TResult directly
    public TResult Map<TResult>(Func<T, TResult> mapFunc)
    {
        if (IsFailure)
        {
            //If it is a failure, it should throw an exception.
            throw new InvalidOperationException("Cannot map a failed result.");
        }

        try
        {
            return mapFunc(Value);
        }
        catch (Exception ex)
        {
            // Handle exceptions during mapping, potentially creating an error result
            throw new InvalidOperationException($"Mapping failed: {ex.Message}");
        }
    }
}