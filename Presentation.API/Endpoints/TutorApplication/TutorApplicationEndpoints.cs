using Application.UseCases.Commands.TutorApplicationOnboarding.Applicant.Step1;
using Application.UseCases.Queries.TutorApplicationOnboarding.Applicant.Common;
using Domain.Payloads;
using Microsoft.AspNetCore.Mvc;
using Presentation.API.Endpoints.TutorApplication.Responses;
using Xcel.Services.Auth.Interfaces.Services;

namespace Presentation.API.Endpoints.TutorApplication;

internal static class TutorApplicationEndpoints
{
    private const string DefaultTag = "tutor application";

    internal static IEndpointRouteBuilder MapTutorApplicationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost(Endpoints.TutorApplications.BasePath, async (
                [AsParameters] CreateTutorApplicationRequest body,
                ITutorApplicationSubmitCommand command,
                HttpContext context) =>
            {
                var documentPayload = await DocumentPayload.FromFileAsync(body.Cv, context.RequestAborted);

                var result = await command.ExecuteAsync(new(
                    body.FirstName,
                    body.LastName,
                    body.EmailAddress,
                    documentPayload), context.RequestAborted);

                return result.IsSuccess
                    ? Results.Created($"/tutor-applications/{result.Value}",
                        result.Map(r => new CreateTutorApplicationResponse(r)))
                    : result.MapProblemDetails();
            })
            .WithName("TutorApplicationOnboarding.Create")
            .WithTags(DefaultTag)
            .DisableAntiforgery()
            .AllowAnonymous()
            .WithSummary("Submit a tutor application.")
            .WithDescription("Allows prospective tutors to submit their application, including personal information and CV.");

        endpoints.MapGet(Endpoints.TutorApplications.My, async (
                IClientInfoService clientInfoService,
                IGetMyTutorApplicationQuery query,
                CancellationToken cancellationToken) =>
            {
                var result = await query.ExecuteAsync(clientInfoService.UserId, cancellationToken);

                return result.IsSuccess
                    ? Results.Ok(GetMyTutorApplicationResponse.FromDomain(result.Value))
                    : result.MapProblemDetails();
            })
            .WithName("TutorApplicationOnboarding.GetMyApplication")
            .WithTags(DefaultTag)
            .RequireAuthorization()
            .WithSummary("Get your tutor application.")
            .WithDescription("Returns the tutor application for the currently authenticated user.");

        return endpoints;
    }
    
    public record CreateTutorApplicationRequest(
        [FromForm] string FirstName,
        [FromForm] string LastName,
        [FromForm] string EmailAddress,
        IFormFile Cv);
}