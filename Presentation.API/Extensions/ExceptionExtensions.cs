using Domain.Results;

namespace Presentation.API.Extensions;

public static class ExceptionExtensions
{
    public static Result MapToResult(this Exception exception) => exception switch
    {
        BadHttpRequestException badHttpRequestException => badHttpRequestException.MapBadHttpRequestException(),
        _ => Result.Fail(new Error(ErrorType.Unexpected, "An unexpected error occurred."))
    };
}