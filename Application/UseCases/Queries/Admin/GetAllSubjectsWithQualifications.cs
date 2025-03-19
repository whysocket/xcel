using Application.UseCases.Shared;
using Domain.Interfaces.Repositories.Shared;

namespace Application.UseCases.Queries.Admin;

public static class GetAllSubjectsWithQualifications
{
    public class Query : PageQuery.IPageQuery, IRequest<Result<Response>>
    {
        public PageRequest PageRequest { get; set; } = new(1, 10);
    }

    public class Validator : PageQuery.Validator<Query>;

    public record Response(IEnumerable<SubjectDto> Subjects, int TotalCount, int Pages);

    public record SubjectDto(Guid Id, string Name, IEnumerable<QualificationDto> Qualifications);

    public record QualificationDto(Guid Id, string Name);

    public class Handler(ISubjectsRepository subjectsRepository) : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var subjectsPage = await subjectsRepository.GetAllWithQualificationsAsync(request.PageRequest, cancellationToken);

            var subjectDtos = subjectsPage.Items.Select(s => new SubjectDto(
                s.Id,
                s.Name,
                s.Qualifications.Select(q => new QualificationDto(q.Id, q.Name))
            ));

            return Result.Ok(new Response(subjectDtos, subjectsPage.Total, subjectsPage.Pages));
        }
    }
}