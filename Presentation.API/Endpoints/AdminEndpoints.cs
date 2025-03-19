using Application.UseCases.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Application.UseCases.Commands.Admin;

namespace Presentation.API.Endpoints;

public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var adminGroup = endpoints.MapGroup("/admin")
            .RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" });

        // Tutor Applicants Endpoints
        var tutorApplicantsGroup = adminGroup.MapGroup("/tutor-applicants");

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
        tutorApplicantsGroup.MapPost("/{tutorId}/reject", async (Guid tutorId, string? rejectionReason, ISender sender) =>
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
            return Results.Ok(result);
        })
        .WithName("GetPendingTutorApplicants")
        .WithTags("Admin", "Tutor Applicants");

        return endpoints;
    }
}