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
        endpoints.MapPost(Endpoints.Moderator.TutorApplications.Approve, async (Guid tutorApplicationId, ISender sender) =>
            {
                var command = new TutorApplicationApproveCv.Command(tutorApplicationId);
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
                async (Guid tutorApplicationId, string? rejectionReason, ISender sender) =>
                {
                    var command = new TutorApplicationRejectCv.Command(tutorApplicationId, rejectionReason);
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

        // Get Specific Tutor Application by Id
        endpoints.MapGet(Endpoints.Moderator.TutorApplications.ById, async (Guid tutorApplicationId, ISender sender) =>
            {
                var result = await sender.Send(new GetPendingCvApplicationById.Query(tutorApplicationId));
                return result.IsSuccess ? Results.Ok(result.Value) : result.MapProblemDetails();
            })
            .WithName("TutorApplicationOnboarding.GetById")
            .WithSummary("Get a specific tutor application by Id.")
            .WithDescription("Retrieves the tutor application and latest CV document by Id if it is in the CV review step.")
            .WithTags(UserRoles.Moderator)
            .RequireAuthorization(p => p.RequireRole(UserRoles.Moderator));

        return endpoints;
    }
}
