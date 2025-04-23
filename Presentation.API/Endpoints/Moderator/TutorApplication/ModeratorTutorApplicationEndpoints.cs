using System.ComponentModel;
using Application.UseCases.Commands.TutorApplicationOnboarding.Moderator.Step2;
using Application.UseCases.Queries.TutorApplicationOnboarding.Moderator.Common;
using Domain.Constants;
using Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.API.Endpoints.Moderator.TutorApplication;

public record TutorApplicationResponse(
    Guid TutorApplicationId,
    TutorApplication.OnboardingStep Step,
    PersonResponse Applicant,
    IEnumerable<TutorDocumentResponse> Documents);

public record PersonResponse(
    string FirstName,
    string LastName,
    string EmailAddress);

public record TutorDocumentResponse(
    Guid DocumentId,
    string Path,
    TutorDocument.TutorDocumentStatus Status,
    TutorDocument.TutorDocumentType Type,
    int Version);

internal static class ModeratorTutorApplicationEndpoints
{
    internal static IEndpointRouteBuilder MapModeratorTutorApplicationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // Approve Tutor Applicant
        endpoints.MapPost(Endpoints.Moderator.TutorApplications.Approve, async (
                [FromRoute, Description("The ID of the tutor application to approve.")] Guid tutorApplicationId,
                IApplicationApproveCvCommand command,
                HttpContext httpContext) =>
            {
                var result = await command.ExecuteAsync(tutorApplicationId, httpContext.RequestAborted);

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
                IApplicationRejectCvCommand command,
                HttpContext httpContext) =>
            {
                var result = await command.ExecuteAsync(tutorApplicationId, rejectionReason, httpContext.RequestAborted);

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
                IGetApplicationsByOnboardingStepQuery query,
                HttpContext httpContext) =>
            {
                var result = await query.ExecuteAsync((Domain.Entities.TutorApplication.OnboardingStep)onboardingStep, httpContext.RequestAborted);
                var mapped = result.Value.Select(app => new TutorApplicationResponse(
                    app.Id,
                    (OnboardingStep)app.CurrentStep,
                    new PersonResponse(
                        app.Applicant.FirstName,
                        app.Applicant.LastName, 
                        app.Applicant.EmailAddress),
                    app.Documents.Select(d => new TutorDocumentResponse(
                        d.Id,
                        d.DocumentPath,
                        d.Status,
                        d.DocumentType,
                        d.Version))
                ));

                return result.IsSuccess ? Results.Ok(mapped) : result.MapProblemDetails();
            })
            .WithName("TutorApplicationOnboarding.GetPending")
            .WithSummary("Get tutor applications by onboarding step.")
            .WithDescription("Returns a list of tutor applications that are currently in the specified onboarding step. Only accessible by moderators or admins.")
            .WithTags(UserRoles.Moderator)
            .RequireAuthorization(p => p.RequireRole(UserRoles.Moderator, UserRoles.Admin));

        // Get Specific Tutor Application by Id
        endpoints.MapGet(Endpoints.Moderator.TutorApplications.ById, async (
                [FromRoute, Description("The ID of the tutor application.")] Guid tutorApplicationId,
                IGetApplicationByIdQuery query) =>
            {
                var result = await query.ExecuteAsync(tutorApplicationId);
                var app = result.Value;

                return result.IsSuccess ? Results.Ok(new TutorApplicationResponse(
                    app.Id,
                    (OnboardingStep)app.CurrentStep,
                    new PersonResponse(
                        app.Applicant.FirstName,
                        app.Applicant.LastName, 
                        app.Applicant.EmailAddress),
                    app.Documents.Select(d => new TutorDocumentResponse(
                        d.Id,
                        d.DocumentPath,
                        d.Status,
                        d.DocumentType,
                        d.Version))
                )) : result.MapProblemDetails();
            })
            .WithName("TutorApplicationOnboarding.GetById")
            .WithSummary("Get a tutor application by ID.")
            .WithDescription("Retrieves a tutor application, including applicant details and all submitted documents. Only accessible by moderators or admins.")
            .WithTags(UserRoles.Moderator)
            .RequireAuthorization(p => p.RequireRole(UserRoles.Moderator, UserRoles.Admin));

        return endpoints;
    }
}

public enum OnboardingStep
{
    CvAnalysis,
    InterviewBookingInterviewBooking,
    DocumentsAnalysis,
    Onboarded
}
