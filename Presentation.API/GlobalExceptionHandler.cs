using Domain.Exceptions;
using Domain.Results;
using Microsoft.AspNetCore.Diagnostics;
using Presentation.API.Extensions;

namespace Presentation.API;

public class GlobalExceptionHandler : IExceptionHandler
{
    public ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var result = MapExceptionToResult(exception);
        var problemDetails = ResultExtensions.CreateProblemDetailsFromDomainResult(result, httpContext);

        httpContext.Response.StatusCode = (int)problemDetails.Status;
        httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken: cancellationToken);

        return ValueTask.FromResult(true);
    }

    private static Result MapExceptionToResult(Exception exception) => exception switch
    {
        DomainValidationException domainValidationException => domainValidationException.ToResult(),
        _ => Result.Fail(new Error(ErrorType.Unexpected, "An unexpected error occurred."))
    };
}