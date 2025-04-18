using Application.UseCases.Commands.TutorApplicationOnboarding.Step3.BookInterview.Reviewer;
using Application.UseCases.Queries.TutorApplicationOnboarding.Common;
using MediatR;
using Xcel.Services.Auth.Constants;

namespace Presentation.API.Endpoints.Reviewer.TutorApplication;

internal static class ReviewerTutorApplicationEndpoints
{
    internal static IEndpointRouteBuilder MapReviewerTutorApplicationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // Reviewer proposes interview dates
        endpoints.MapPost(Endpoints.Reviewer.TutorApplications.ProposeDates,
                async (Guid tutorApplicationId, ReviewerProposeInterviewDatesBody body, ISender sender) =>
                {
                    var command = new ReviewerProposeInterviewDates.Command(tutorApplicationId, body.ProposedDates, body.Observations);
                    var result = await sender.Send(command);
                    return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.Errors);
                })
            .WithName("Reviewer.ProposeInterviewDates")
            .WithSummary("Reviewer proposes interview dates")
            .WithDescription("Allows the reviewer to propose up to 3 interview dates to the tutor.")
            .WithTags(UserRoles.Reviewer)
            .RequireAuthorization(p => p.RequireRole(UserRoles.Reviewer));

        // Reviewer gets interview details
        endpoints.MapGet(Endpoints.Reviewer.TutorApplications.GetInterviewDetails,
                async (Guid tutorApplicationId, ISender sender, HttpContext context) =>
                {
                    var userId = Guid.Parse(context.User.Identity!.Name!);
                    var query = new GetInterviewDetailsByParty.Query(tutorApplicationId, userId,
                        GetInterviewDetailsByParty.Party.Reviewer);
                    var result = await sender.Send(query);
                    return result.IsSuccess ? Results.Ok(result.Value) : result.MapProblemDetails();
                })
            .WithName("Reviewer.GetInterviewDetails")
            .WithSummary("Get interview details")
            .WithDescription("Retrieves interview details, including proposed dates and observations.")
            .WithTags(UserRoles.Reviewer)
            .RequireAuthorization(p => p.RequireRole(UserRoles.Reviewer));

        return endpoints;
    }

    public record ReviewerProposeInterviewDatesBody(List<DateTime> ProposedDates, string? Observations);
}