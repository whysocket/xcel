﻿namespace Domain.Results;

public enum ErrorType
{
    Unexpected = 0,
    Validation = 1,
    NotFound = 2,
    Unauthorized = 3,
    Forbidden = 4,
    Conflict = 5,
}

public static class Errors
{
    public static Error Unexpected => new(ErrorType.Unexpected, "Unexpected error.");
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

    public static Result Ok() => new([]);

    public static Result Fail(Error error) => new(new List<Error> { error });

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

    public static Result<T> Ok(T value) => new(value, []);

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

    public string ErrorMessages =>
        Errors.Count == 0
            ? throw new InvalidOperationException($"No errors found.")
            : string.Join(Environment.NewLine, Errors.Select(error => error.Message));
}
