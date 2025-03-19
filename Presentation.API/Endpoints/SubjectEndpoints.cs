using Application.UseCases.Queries.Admin;
using Domain.Interfaces.Repositories.Shared;
using MediatR;
using Presentation.API.Extensions;

namespace Presentation.API.Endpoints;

public static class SubjectEndpoints
{
    public static IEndpointRouteBuilder MapSubjectEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // Subjects and Qualifications Endpoints
        var subjectsGroup = endpoints.MapGroup("/subjects-with-qualifications");

        // Get All Subjects with Qualifications
        subjectsGroup.MapGet("/", async (HttpContext context, ISender sender, int page = 1, int pageSize = 10) =>
            {
                var query = new GetAllSubjectsWithQualifications.Query
                {
                    PageRequest = new PageRequest(page, pageSize)
                };

                var result = await sender.Send(query);

                return result.IsSuccess ? Results.Ok(result.Value) : result.MapProblemDetails();
            })
            .WithName("GetAllSubjectsWithQualifications")
            .WithTags("Subjects");

        return endpoints;
    }
}

