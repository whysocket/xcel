namespace Application.UseCases.Queries.TutorApplicationOnboarding.Reviewer.Step3;

/// <summary>
/// Allows a reviewer to fetch all tutor applications they are assigned to as interviewer.
/// </summary>
public interface IGetReviewerAssignedInterviewsQuery
{
    Task<Result<List<TutorApplication>>> ExecuteAsync(
        Guid reviewerId,
        CancellationToken cancellationToken = default
    );
}

internal sealed class GetReviewerAssignedInterviewsQuery(
    ITutorApplicationsRepository tutorApplicationsRepository,
    ILogger<GetReviewerAssignedInterviewsQuery> logger
) : IGetReviewerAssignedInterviewsQuery
{
    private const string ServiceName = "[GetReviewerAssignedInterviewsQuery]";

    public async Task<Result<List<TutorApplication>>> ExecuteAsync(
        Guid reviewerId,
        CancellationToken cancellationToken = default
    )
    {
        logger.LogInformation(
            "{Service} Fetching assigned interviews for reviewer {ReviewerId}",
            ServiceName,
            reviewerId
        );

        var applications = await tutorApplicationsRepository.GetAllByReviewerIdAsync(
            reviewerId,
            cancellationToken
        );

        logger.LogInformation(
            "{Service} Found {Count} interviews for reviewer {ReviewerId}",
            ServiceName,
            applications.Count,
            reviewerId
        );

        return Result.Ok(applications);
    }
}
