using System.Net;
using Domain.Results;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Presentation.API.Extensions;

public class CustomProblemDetails(HttpStatusCode status, string path, IEnumerable<object>? errors = null)
{
    public HttpStatusCode Status { get; set; } = status;
    public string Path { get; set; } = path;
    public IEnumerable<object>? Errors { get; set; } = errors;
}

public static class ResultExtensions
{
    public static JsonHttpResult<CustomProblemDetails> MapProblemDetails<T>(this Result<T> result) =>
        MapProblemDetails(Result.Fail(result.Errors));

    public static JsonHttpResult<CustomProblemDetails> MapProblemDetails(this Result result)
    {
        var problemDetails = CreateProblemDetailsFromDomainResult(result, new DefaultHttpContext());

        return TypedResults.Json(problemDetails, statusCode: (int)problemDetails.Status);
    }

    public static CustomProblemDetails CreateProblemDetailsFromDomainResult(Result result, HttpContext httpContext)
    {
        if (result.Errors.Count == 0)
        {
            return new(HttpStatusCode.InternalServerError, $"{httpContext.Request.Method} {httpContext.Request.Path}");
        }

        var firstError = result.Errors[0];
        var statusCode = firstError.Type.MapStatusCode();

        return new(
            statusCode,
            $"{httpContext.Request.Method} {httpContext.Request.Path}",
            result.Errors.Select(e => new { message = e.Message, type = e.Type.ToString() })
        );
    }

    private static HttpStatusCode MapStatusCode(this ErrorType errorType) => errorType switch
    {
        ErrorType.Unexpected => HttpStatusCode.InternalServerError,
        ErrorType.Validation => HttpStatusCode.BadRequest,
        ErrorType.NotFound => HttpStatusCode.NotFound,
        ErrorType.Unauthorized => HttpStatusCode.Unauthorized,
        ErrorType.Conflict => HttpStatusCode.Conflict,
        _ => HttpStatusCode.InternalServerError
    };
}