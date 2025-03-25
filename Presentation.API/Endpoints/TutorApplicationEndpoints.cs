using Application.UseCases.Commands;
using Domain.Payloads;
using MediatR;
using Presentation.API.Endpoints.TutorApplication;

namespace Presentation.API.Endpoints;

public static class TutorApplicationEndpoints
{
    public static IEndpointRouteBuilder MapTutorApplicationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/tutor-applications", async (
                [AsParameters] CreateTutorApplicationRequest body,
                ISender sender,
                HttpContext context) =>
            {
                var documentPayload = await DocumentPayload.FromFileAsync(body.Cv, context.RequestAborted);

                var command = new TutorInitialApplicationSubmission.Command(
                    body.FirstName,
                    body.LastName,
                    body.EmailAddress,
                    documentPayload);

                var result = await sender.Send(command);

                return result.IsSuccess
                    ? Results.Created($"/tutor-applications/{result.Value}", result.Map(r => new CreateTutorApplicationResponse(r)))
                    : result.MapProblemDetails();
            })
            .WithName("SubmitTutorApplication")
            .WithTags("Tutor Applications")
            .DisableAntiforgery();

        return endpoints;
    }
}