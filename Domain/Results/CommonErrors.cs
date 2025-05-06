namespace Domain.Results;

public static class CommonErrors
{
    public static Error InvalidGuid(string paramName)
    {
        return new Error(ErrorType.Validation, $"'{paramName}' must be a valid GUID.");
    }
}
