using Application.UseCases.Commands;
using Domain.Payloads;
using MediatR;
using Presentation.API.Endpoints.TutorApplication.Requests;
using Presentation.API.Endpoints.TutorApplication.Responses;

namespace Presentation.API.Endpoints.TutorApplication;

internal static class TutorApplicationEndpoints
{
    private const string DefaultTag = "Tutor Application";

    internal static IEndpointRouteBuilder MapTutorApplicationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost(Endpoints.TutorApplications.BasePath, async (
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
                    ? Results.Created($"/tutor-applications/{result.Value}",
                        result.Map(r => new CreateTutorApplicationResponse(r)))
                    : result.MapProblemDetails();
            })
            .WithName("TutorApplications.Create")
            .WithTags(DefaultTag)
            .DisableAntiforgery()
            .AllowAnonymous()
            .WithSummary("Submit a tutor application.")
            .WithDescription("Allows prospective tutors to submit their application, including personal information and CV.");

        return endpoints;
    }
}