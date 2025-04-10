using Application.UseCases.Commands;
using Application.UseCases.Commands.Moderator;
using Application.UseCases.Queries;
using Domain.Payloads;
using MediatR;
using Presentation.API.Endpoints.TutorApplication.Requests;
using Presentation.API.Endpoints.TutorApplication.Responses;
using Xcel.Services.Auth.Constants;

namespace Presentation.API.Endpoints.TutorApplication;

internal static class TutorApplicationEndpoints
{
    internal static IEndpointRouteBuilder MapTutorApplicationEndpoints(this IEndpointRouteBuilder endpoints)
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
                    ? Results.Created($"/tutor-applications/{result.Value}",
                        result.Map(r => new CreateTutorApplicationResponse(r)))
                    : result.MapProblemDetails();
            })
            .WithName("TutorApplications.Create")
            .WithTags("Tutor Applicants")
            .DisableAntiforgery()
            .AllowAnonymous();

        return endpoints;
    }

    internal static RouteGroupBuilder MapModeratorTutorApplicationEndpoints(this RouteGroupBuilder tutorApplicantsGroup)
    {
        // Approve Tutor Applicant
        tutorApplicantsGroup.MapPost("/{tutorId}/approve", async (Guid tutorId, ISender sender) =>
            {
                var command = new ApproveTutorApplicant.Command(tutorId);
                var result = await sender.Send(command);

                return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.Errors);
            })
            .WithName("TutorApplications.Approve")
            .WithTags("Moderator", "Tutor Applicants")
            .RequireAuthorization(p => p.RequireRole(Roles.Moderator));

        // Reject Tutor Applicant
        tutorApplicantsGroup.MapPost("/{tutorId}/reject",
                async (Guid tutorId, string? rejectionReason, ISender sender) =>
                {
                    var command = new RejectTutorApplicant.Command(tutorId, rejectionReason);
                    var result = await sender.Send(command);

                    return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.Errors);
                })
            .WithName("TutorApplications.Reject")
            .WithTags("Moderator", "Tutor Applicants")
            .RequireAuthorization(p => p.RequireRole(Roles.Moderator));

        // Get Pending Tutor Applicants
        tutorApplicantsGroup.MapGet("/", async (ISender sender) =>
            {
                var result = await sender.Send(new GetPendingTutorsApplicants.Query());
                return result.IsSuccess ? Results.Ok(result.Value) : result.MapProblemDetails();
            })
            .WithName("TutorApplications.GetPending")
            .WithTags("Moderator", "Tutor Applicants")
            .RequireAuthorization(p => p.RequireRole(Roles.Moderator));
        
        return tutorApplicantsGroup;
    }
}