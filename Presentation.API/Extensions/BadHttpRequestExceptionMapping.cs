using Domain.Results;

namespace Presentation.API.Extensions;

public static class BadHttpRequestExceptionMapping
{
    public static Result MapBadHttpRequestException(this BadHttpRequestException exception)
    {
        var errorMessage = exception.Message;

        if (errorMessage.Contains("Required parameter \"IFormFile"))
        {
            return MapMissingIFormFile(errorMessage);
        }

        if (errorMessage.Contains("Invalid request headers"))
        {
            return Result.Fail(new Error(ErrorType.Validation, "Invalid request headers."));
        }

        if (errorMessage.Contains("Failed to read request body"))
        {
            return Result.Fail(new Error(ErrorType.Validation, "Failed to read request body."));
        }

        return Result.Fail(new Error(ErrorType.Validation, errorMessage));
    }

    private static Result MapMissingIFormFile(string errorMessage)
    {
        const string formFileString = "IFormFile ";
        const string requiredParameterString = "Required parameter \"";

        if (errorMessage.Contains(requiredParameterString + formFileString, StringComparison.Ordinal))
        {
            var startIndex = errorMessage.IndexOf(formFileString, StringComparison.Ordinal) + formFileString.Length;
            var endIndex = errorMessage.IndexOf('"', startIndex);

            if (startIndex >= 0 && endIndex > startIndex)
            {
                var parameterName = errorMessage.Substring(startIndex, endIndex - startIndex);

                return Result.Fail(new Error(ErrorType.Validation, $"Please upload the {parameterName} file."));
            }
        }

        return Result.Fail(new Error(ErrorType.Validation, errorMessage));
    }
}
