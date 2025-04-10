using Application.UseCases.Queries;
using Domain.Interfaces.Repositories.Shared;
using MediatR;

namespace Presentation.API.Endpoints.Subjects;

public static class SubjectEndpoints
{
    public static IEndpointRouteBuilder MapSubjectEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var subjectsGroup = endpoints.MapGroup("/subjects");

        subjectsGroup.MapGet("/qualifications", async (ISender sender, int page = 1, int pageSize = 10) =>
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

