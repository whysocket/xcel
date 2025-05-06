namespace Application.UseCases.Queries.TutorApplicationOnboarding.Moderator.Common;

public interface IGetApplicationByIdQuery
{
    Task<Result<TutorApplication>> ExecuteAsync(
        Guid tutorApplicationId,
        CancellationToken cancellationToken = default
    );
}

internal static class GetApplicationByIdQueryErrors
{
    public static Error NotFound(Guid id) =>
        new(ErrorType.NotFound, $"Tutor application with ID '{id}' not found.");
}

internal sealed class GetApplicationByIdQuery(
    ITutorApplicationsRepository tutorApplicationsRepository,
    ILogger<GetApplicationByIdQuery> logger
) : IGetApplicationByIdQuery
{
    private const string ServiceName = "[GetApplicationByIdQuery]";

    public async Task<Result<TutorApplication>> ExecuteAsync(
        Guid tutorApplicationId,
        CancellationToken cancellationToken = default
    )
    {
        logger.LogInformation(
            "{Service} Fetching tutor application with ID {Id}",
            ServiceName,
            tutorApplicationId
        );

        var application = await tutorApplicationsRepository.GetByIdWithDocumentsAndApplicantAsync(
            tutorApplicationId,
            cancellationToken
        );
        if (application is null)
        {
            logger.LogWarning(
                "{Service} Tutor application with ID {Id} not found",
                ServiceName,
                tutorApplicationId
            );
            return Result.Fail<TutorApplication>(
                GetApplicationByIdQueryErrors.NotFound(tutorApplicationId)
            );
        }

        logger.LogInformation(
            "{Service} Found tutor application with ID {Id}",
            ServiceName,
            tutorApplicationId
        );
        return Result.Ok(application);
    }
}
