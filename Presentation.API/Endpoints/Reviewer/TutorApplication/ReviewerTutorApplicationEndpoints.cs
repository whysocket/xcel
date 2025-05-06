using System.ComponentModel;
using System.Text.Json.Serialization;
using Application.UseCases.Commands.TutorApplicationOnboarding.Reviewer.Step3.BookInterview;
using Application.UseCases.Queries.TutorApplicationOnboarding.Reviewer.Step3;
using Domain.Constants;
using Microsoft.AspNetCore.Mvc;
using Xcel.Services.Auth.Interfaces.Services;

namespace Presentation.API.Endpoints.Reviewer.TutorApplication;

internal static class ReviewerTutorApplicationEndpoints
{
    internal static IEndpointRouteBuilder MapReviewerTutorApplicationEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        // Reviewer requests interview reschedule
        endpoints
            .MapPost(
                Endpoints.Reviewer.TutorApplications.Reschedule,
                async (
                    Guid tutorApplicationId,
                    [FromBody] ReviewerRequestRescheduleInputRequest body,
                    IReviewerRequestInterviewRescheduleCommand command
                ) =>
                {
                    var input = new ReviewerRequestInterviewRescheduleInput(
                        tutorApplicationId,
                        body.RescheduleReason
                    );
                    var result = await command.ExecuteAsync(input);
                    return result.IsSuccess ? Results.Ok() : result.MapProblemDetails();
                }
            )
            .WithName("Reviewer.RequestInterviewReschedule")
            .WithSummary("Request interview reschedule")
            .WithDescription(
                "Allows the reviewer to request a new interview slot from the applicant."
            )
            .WithTags(UserRoles.Reviewer)
            .RequireAuthorization(p => p.RequireRole(UserRoles.Reviewer));

        // Reviewer gets list of interviews assigned to them
        endpoints
            .MapGet(
                Endpoints.Reviewer.TutorApplications.GetAssignedInterviews,
                async (
                    IClientInfoService clientInfoService,
                    IGetReviewerAssignedInterviewsQuery query
                ) =>
                {
                    var result = await query.ExecuteAsync(clientInfoService.UserId);

                    if (result.IsFailure)
                    {
                        return result.MapProblemDetails();
                    }

                    var mapped = result.Value.Select(i => new AssignedInterviewResponse(
                        i.ApplicantId,
                        i.Applicant.FullName,
                        i.Interview!.ScheduledAtUtc,
                        i.Interview.Status switch
                        {
                            Domain
                                .Entities
                                .TutorApplicationInterview
                                .InterviewStatus
                                .AwaitingApplicantSlotSelection =>
                                InterviewStatusResponse.AwaitingApplicantSlotSelection,
                            Domain.Entities.TutorApplicationInterview.InterviewStatus.Confirmed =>
                                InterviewStatusResponse.Confirmed,
                            _ => throw new ArgumentOutOfRangeException(
                                nameof(i.Interview.Status),
                                $"Unsupported status: {i.Interview.Status}"
                            ),
                        }
                    ));

                    return Results.Ok(mapped);
                }
            )
            .WithName("Reviewer.GetAssignedInterviews")
            .WithSummary("Get assigned interviews")
            .WithDescription(
                "Retrieves all tutor applications where the authenticated reviewer is the assigned interviewer."
            )
            .WithTags(UserRoles.Reviewer)
            .RequireAuthorization(p => p.RequireRole(UserRoles.Reviewer));

        return endpoints;
    }

    public record ReviewerRequestRescheduleInputRequest(
        [property: Description(
            "Optional reason provided by the reviewer when requesting to reschedule the interview."
        )]
            string? RescheduleReason
    );

    public record AssignedInterviewResponse(
        Guid TutorApplicationId,
        string ApplicantFullName,
        DateTime? ScheduledAtUtc,
        InterviewStatusResponse InterviewStatus
    );

    [JsonConverter(typeof(JsonStringEnumConverter<InterviewStatusResponse>))]
    public enum InterviewStatusResponse
    {
        AwaitingApplicantSlotSelection,
        Confirmed,
    }
}
