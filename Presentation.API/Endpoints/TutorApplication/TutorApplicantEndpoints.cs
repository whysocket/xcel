using Application.UseCases.Commands;
using Application.UseCases.Commands.Admin;
using Application.UseCases.Queries;
using Domain.Payloads;
using MediatR;

namespace Presentation.API.Endpoints.TutorApplication;

internal static class TutorApplicantEndpoints
{
    internal static IEndpointRouteBuilder MapTutorApplicantEndpoints(this IEndpointRouteBuilder endpoints)
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
            .WithTags("Tutor Applicants")
            .DisableAntiforgery();

        return endpoints;
    }

    internal static void MapAdminTutorApplicantEndpoints(this RouteGroupBuilder tutorApplicantsGroup)
    {
        // Approve Tutor Applicant
        tutorApplicantsGroup.MapPost("/{tutorId}/approve", async (Guid tutorId, ISender sender) =>
            {
                var command = new ApproveTutorApplicant.Command(tutorId);
                var result = await sender.Send(command);

                return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.Errors);
            })
            .WithName("ApproveTutorApplicant")
            .WithTags("Admin", "Tutor Applicants");

        // Reject Tutor Applicant
        tutorApplicantsGroup.MapPost("/{tutorId}/reject",
                async (Guid tutorId, string? rejectionReason, ISender sender) =>
                {
                    var command = new RejectTutorApplicant.Command(tutorId, rejectionReason);
                    var result = await sender.Send(command);

                    return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.Errors);
                })
            .WithName("RejectTutorApplicant")
            .WithTags("Admin", "Tutor Applicants");

        // Get Pending Tutor Applicants
        tutorApplicantsGroup.MapGet("/", async (ISender sender) =>
            {
                var result = await sender.Send(new GetPendingTutorsApplicants.Query());
                return result.IsSuccess ? Results.Ok(result.Value) : result.MapProblemDetails();
            })
            .WithName("GetPendingTutorApplicants")
            .WithTags("Admin", "Tutor Applicants");
    }
}