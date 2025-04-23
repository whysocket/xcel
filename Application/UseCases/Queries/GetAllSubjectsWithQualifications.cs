using Domain.Interfaces.Repositories.Shared;

namespace Application.UseCases.Queries;

public interface IGetAllSubjectsWithQualificationsQuery
{
    Task<Result<(List<Subject> Subjects, int TotalCount, int Pages)>> ExecuteAsync(PageRequest pageRequest, CancellationToken cancellationToken = default);
}

internal sealed class GetAllSubjectsWithQualificationsQuery(
    ISubjectsRepository subjectsRepository
) : IGetAllSubjectsWithQualificationsQuery
{
    private const string ServiceName = "[GetAllSubjectsWithQualificationsQuery]";

    public async Task<Result<(List<Subject> Subjects, int TotalCount, int Pages)>> ExecuteAsync(PageRequest pageRequest, CancellationToken cancellationToken = default)
    {
        var subjectsPage = await subjectsRepository.GetAllWithQualificationsAsync(pageRequest, cancellationToken);

        return Result.Ok((subjectsPage.Items, subjectsPage.TotalCount, subjectsPage.Pages));
    }
}