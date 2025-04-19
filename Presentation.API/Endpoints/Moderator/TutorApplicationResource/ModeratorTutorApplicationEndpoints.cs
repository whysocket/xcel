using System.ComponentModel;
using System.Text.Json.Serialization;
using Application.UseCases.Commands.TutorApplicationOnboarding.Step2;
using Application.UseCases.Queries.TutorApplicationOnboarding.Moderator;
using Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.API.Endpoints.Moderator.TutorApplicationResource;

[JsonConverter(typeof(JsonStringEnumConverter<OnboardingStep>))]
public enum OnboardingStep
{
    CvUnderReview,
    AwaitingInterviewBooking,
    InterviewScheduled,
    DocumentsRequested,
    Onboarded,
}

internal static class ModeratorTutorApplicationEndpoints
{
    internal static IEndpointRouteBuilder MapModeratorTutorApplicationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // Approve Tutor Applicant
        endpoints.MapPost(Endpoints.Moderator.TutorApplications.Approve, async (
                [FromRoute, Description("The ID of the tutor application to approve.")] Guid tutorApplicationId,
                ISender sender) =>
            {
                var command = new TutorApplicationApproveCv.Command(tutorApplicationId);
                var result = await sender.Send(command);

                return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.Errors);
            })
            .WithName("TutorApplicationOnboarding.Approve")
            .WithSummary("Approve a tutor application.")
            .WithDescription("Approves a tutor application currently in the CV review step. Only accessible by moderators or admins.")
            .WithTags(UserRoles.Moderator)
            .RequireAuthorization(p => p.RequireRole(UserRoles.Moderator, UserRoles.Admin));

        // Reject Tutor Applicant
        endpoints.MapPost(Endpoints.Moderator.TutorApplications.Reject, async (
                [FromRoute, Description("The ID of the tutor application to reject.")] Guid tutorApplicationId,
                [FromBody, Description("An optional reason for rejection.")] string? rejectionReason,
                ISender sender) =>
            {
                var command = new TutorApplicationRejectCv.Command(tutorApplicationId, rejectionReason);
                var result = await sender.Send(command);

                return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.Errors);
            })
            .WithName("TutorApplicationOnboarding.Reject")
            .WithSummary("Reject a tutor application.")
            .WithDescription("Rejects a tutor application currently in the CV review step, optionally including a reason. Only accessible by moderators or admins.")
            .WithTags(UserRoles.Moderator)
            .RequireAuthorization(p => p.RequireRole(UserRoles.Moderator, UserRoles.Admin));

        // Get Pending Tutor Applicants by Step
        endpoints.MapGet(Endpoints.Moderator.TutorApplications.BasePath, async (
                [FromQuery, Description("The onboarding step to filter tutor applications by.")] OnboardingStep onboardingStep,
                ISender sender) =>
            {
                var domainStep = Enum.Parse<Domain.Entities.TutorApplication.OnboardingStep>(onboardingStep.ToString());
                var query = new GetApplicationsByOnboardingStep.Query(domainStep);
                
                var result = await sender.Send(query);
                return result.IsSuccess ? Results.Ok(result.Value) : result.MapProblemDetails();
            })
            .WithName("TutorApplicationOnboarding.GetPending")
            .WithSummary("Get tutor applications by onboarding step.")
            .WithDescription("Returns a list of tutor applications that are currently in the specified onboarding step. Only accessible by moderators or admins.")
            .WithTags(UserRoles.Moderator)
            .RequireAuthorization(p => p.RequireRole(UserRoles.Moderator, UserRoles.Admin));

        // Get Specific Tutor Application by Id
        endpoints.MapGet(Endpoints.Moderator.TutorApplications.ById, async (
                [FromRoute, Description("The ID of the tutor application.")] Guid tutorApplicationId,
                ISender sender) =>
            {
                var result = await sender.Send(new GetApplicationById.Query(tutorApplicationId));

                return result.IsSuccess ? Results.Ok(result.Value) : result.MapProblemDetails();
            })
            .WithName("TutorApplicationOnboarding.GetById")
            .WithSummary("Get a tutor application by ID and step.")
            .WithDescription("Retrieves a tutor application, including applicant details and all submitted documents. Only accessible by moderators or admins.")
            .WithTags(UserRoles.Moderator)
            .RequireAuthorization(p => p.RequireRole(UserRoles.Moderator, UserRoles.Admin));

        return endpoints;
    }
}