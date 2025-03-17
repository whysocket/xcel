namespace Domain.Exceptions;

public class DomainValidationException(Dictionary<string, List<string>> errors) : Exception
{
    public Dictionary<string, List<string>> Errors { get; } = errors;
}