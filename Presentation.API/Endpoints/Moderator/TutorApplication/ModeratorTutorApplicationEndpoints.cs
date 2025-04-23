using System.ComponentModel;
using System.Text.Json.Serialization;
using Application.UseCases.Commands.TutorApplicationOnboarding.Moderator.Step2;
using Application.UseCases.Queries.TutorApplicationOnboarding.Moderator.Common;
using Domain.Constants;
using Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Presentation.API.Endpoints.Reviewer.TutorApplication;

namespace Presentation.API.Endpoints.Moderator.TutorApplication;

public record TutorApplicationResponse(
    Guid TutorApplicationId,
    OnboardingStep Step,
    PersonResponse Applicant,
    IEnumerable<TutorDocumentResponse> Documents,
    InterviewDocumentResponse? Interview);

public record PersonResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string EmailAddress);

public record TutorDocumentResponse(
    Guid Id,
    string Path,
    TutorDocumentStatus Status,
    TutorDocumentType Type,
    int Version);

public record InterviewDocumentResponse(
    ReviewerResponse Reviewer);

public record ReviewerResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string EmailAddress);

internal static class ModeratorTutorApplicationEndpoints
{
    internal static IEndpointRouteBuilder MapModeratorTutorApplicationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // Approve Tutor Applicant
        endpoints.MapPost(Endpoints.Moderator.TutorApplications.Approve, async (
                [FromRoute, Description("The ID of the tutor application to approve.")]
                Guid tutorApplicationId,
                IApplicationApproveCvCommand command,
                HttpContext httpContext) =>
            {
                var result = await command.ExecuteAsync(tutorApplicationId, httpContext.RequestAborted);

                return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.Errors);
            })
            .WithName("TutorApplicationOnboarding.Approve")
            .WithSummary("Approve a tutor application.")
            .WithDescription(
                "Approves a tutor application currently in the CV review step. Only accessible by moderators or admins.")
            .WithTags(UserRoles.Moderator)
            .RequireAuthorization(p => p.RequireRole(UserRoles.Moderator, UserRoles.Admin));

        // Reject Tutor Applicant
        endpoints.MapPost(Endpoints.Moderator.TutorApplications.Reject, async (
                [FromRoute, Description("The ID of the tutor application to reject.")]
                Guid tutorApplicationId,
                [FromBody, Description("An optional reason for rejection.")]
                string? rejectionReason,
                IApplicationRejectCvCommand command,
                HttpContext httpContext) =>
            {
                var result =
                    await command.ExecuteAsync(tutorApplicationId, rejectionReason, httpContext.RequestAborted);

                return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.Errors);
            })
            .WithName("TutorApplicationOnboarding.Reject")
            .WithSummary("Reject a tutor application.")
            .WithDescription(
                "Rejects a tutor application currently in the CV review step, optionally including a reason. Only accessible by moderators or admins.")
            .WithTags(UserRoles.Moderator)
            .RequireAuthorization(p => p.RequireRole(UserRoles.Moderator, UserRoles.Admin));

        // Get Pending Tutor Applicants by Step
        endpoints.MapGet(Endpoints.Moderator.TutorApplications.BasePath, async (
                [FromQuery, Description("The onboarding step to filter tutor applications by.")]
                OnboardingStep onboardingStep,
                IGetApplicationsByOnboardingStepQuery query,
                HttpContext httpContext) =>
            {
                var result = await query.ExecuteAsync((Domain.Entities.TutorApplication.OnboardingStep)onboardingStep,
                    httpContext.RequestAborted);
             
                var mapped = result.Value.Select(TutorApplicationMapper.MapToResponse).ToList();

                return result.IsSuccess ? Results.Ok(mapped) : result.MapProblemDetails();
            })
            .WithName("TutorApplicationOnboarding.GetPending")
            .WithSummary("Get tutor applications by onboarding step.")
            .WithDescription(
                "Returns a list of tutor applications that are currently in the specified onboarding step. Only accessible by moderators or admins.")
            .WithTags(UserRoles.Moderator)
            .RequireAuthorization(p => p.RequireRole(UserRoles.Moderator, UserRoles.Admin));

        // Get Specific Tutor Application by Id
        endpoints.MapGet(Endpoints.Moderator.TutorApplications.ById, async (
                [FromRoute, Description("The ID of the tutor application.")]
                Guid tutorApplicationId,
                IGetApplicationByIdQuery query) =>
            {
                var result = await query.ExecuteAsync(tutorApplicationId);

                return result.IsSuccess
                    ? Results.Ok(TutorApplicationMapper.MapToResponse(result.Value))
                    : result.MapProblemDetails();
            })
            .WithName("TutorApplicationOnboarding.GetById")
            .WithSummary("Get a tutor application by ID.")
            .WithDescription(
                "Retrieves a tutor application, including applicant details and all submitted documents. Only accessible by moderators or admins.")
            .WithTags(UserRoles.Moderator)
            .RequireAuthorization(p => p.RequireRole(UserRoles.Moderator, UserRoles.Admin));

        return endpoints;
    }
}

[JsonConverter(typeof(JsonStringEnumConverter<OnboardingStep>))]
public enum OnboardingStep
{
    CvAnalysis,
    InterviewBookingInterviewBooking,
    DocumentsAnalysis,
    Onboarded
}

[JsonConverter(typeof(JsonStringEnumConverter<TutorDocumentStatus>))]
public enum TutorDocumentStatus
{
    Pending,
    Approved,
    ResubmissionNeeded
}

[JsonConverter(typeof(JsonStringEnumConverter<TutorDocumentType>))]
public enum TutorDocumentType
{
    Cv,
    Id,
    Dbs,
    Other
}

public static class TutorApplicationMapper
{
    public static TutorApplicationResponse MapToResponse(Domain.Entities.TutorApplication application)
    {
        return new TutorApplicationResponse(
            application.Id,
            (OnboardingStep)application.CurrentStep,
            MapToPersonResponse(application.Applicant),
            application.Documents.Select(MapToTutorDocumentResponse).ToList(),
            application.Interview is not null ? MapToInterviewDocumentResponse(application.Interview) : null
        );
    }

    public static PersonResponse MapToPersonResponse(Person person)
    {
        return new PersonResponse(
            person.Id,
            person.FirstName,
            person.LastName,
            person.EmailAddress
        );
    }

    public static TutorDocumentResponse MapToTutorDocumentResponse(TutorDocument document)
    {
        return new TutorDocumentResponse(
            document.Id,
            document.DocumentPath,
            (TutorDocumentStatus)document.Status,
            (TutorDocumentType)document.DocumentType,
            document.Version
        );
    }

    public static InterviewDocumentResponse MapToInterviewDocumentResponse(TutorApplicationInterview interview)
    {
        return new InterviewDocumentResponse(
            MapToReviewerResponse(interview.Reviewer)
        );
    }

    public static ReviewerResponse MapToReviewerResponse(Person reviewer)
    {
        return new ReviewerResponse(
            reviewer.Id,
            reviewer.FirstName,
            reviewer.LastName,
            reviewer.EmailAddress
        );
    }
}
