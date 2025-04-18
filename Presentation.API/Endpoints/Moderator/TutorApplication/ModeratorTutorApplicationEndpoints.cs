using Application.UseCases.Commands.TutorApplicationOnboarding.Step2;
using Application.UseCases.Queries.TutorApplicationOnboarding.Moderator;
using MediatR;
using Xcel.Services.Auth.Constants;

namespace Presentation.API.Endpoints.Moderator.TutorApplication;

internal static class ModeratorTutorApplicationEndpoints
{
    internal static IEndpointRouteBuilder MapModeratorTutorApplicationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // Approve Tutor Applicant
        endpoints.MapPost(Endpoints.Moderator.TutorApplications.Approve, async (Guid tutorId, ISender sender) =>
            {
                var command = new TutorApplicationApproveCv.Command(tutorId);
                var result = await sender.Send(command);

                return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.Errors);
            })
            .WithName("TutorApplicationOnboarding.Approve")
            .WithSummary("Approve a tutor application.")
            .WithDescription("Approves a pending tutor application by the moderator.")
            .WithTags(UserRoles.Moderator)
            .RequireAuthorization(p => p.RequireRole(UserRoles.Moderator));

        // Reject Tutor Applicant
        endpoints.MapPost(Endpoints.Moderator.TutorApplications.Reject,
                async (Guid tutorId, string? rejectionReason, ISender sender) =>
                {
                    var command = new TutorApplicationRejectCv.Command(tutorId, rejectionReason);
                    var result = await sender.Send(command);

                    return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.Errors);
                })
            .WithName("TutorApplicationOnboarding.Reject")
            .WithSummary("Reject a tutor application.")
            .WithDescription("Rejects a pending tutor application by the moderator, optionally providing a rejection reason.")
            .WithTags(UserRoles.Moderator)
            .RequireAuthorization(p => p.RequireRole(UserRoles.Moderator));

        // Get Pending Tutor Applicants
        endpoints.MapGet(Endpoints.Moderator.TutorApplications.BasePath, async (ISender sender) =>
            {
                var result = await sender.Send(new GetPendingCvApplications.Query());
                return result.IsSuccess ? Results.Ok(result.Value) : result.MapProblemDetails();
            })
            .WithName("TutorApplicationOnboarding.GetPending")
            .WithSummary("Get pending tutor applications.")
            .WithDescription("Retrieves a list of all pending tutor applications for moderator review.")
            .WithTags(UserRoles.Moderator)
            .RequireAuthorization(p => p.RequireRole(UserRoles.Moderator));
        
        return endpoints;
    }
}
