using Domain.Results;

namespace Domain.Exceptions;

public class DomainValidationException(Dictionary<string, List<string>> errors) : Exception
{
    public Dictionary<string, List<string>> Errors { get; } = errors;

    public Result ToResult()
    {
        return new (Errors.SelectMany(kvp => kvp.Value.Select(error => new Error(ErrorType.Validation, error))).ToList());
    }
}