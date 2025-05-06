namespace Application.UseCases.Queries.TutorApplicationOnboarding.Moderator.Common;

public interface IGetApplicationsByOnboardingStepQuery
{
    Task<Result<List<TutorApplication>>> ExecuteAsync(
        TutorApplication.OnboardingStep onboardingStep,
        CancellationToken cancellationToken = default
    );
}

internal sealed class GetApplicationsByOnboardingStepQuery(
    ITutorApplicationsRepository tutorApplicationsRepository,
    ILogger<GetApplicationsByOnboardingStepQuery> logger
) : IGetApplicationsByOnboardingStepQuery
{
    private const string ServiceName = "[GetApplicationsByOnboardingStepQuery]";

    public async Task<Result<List<TutorApplication>>> ExecuteAsync(
        TutorApplication.OnboardingStep onboardingStep,
        CancellationToken cancellationToken = default
    )
    {
        logger.LogInformation(
            "{Service} Fetching tutor applications at step: {Step}",
            ServiceName,
            onboardingStep
        );

        var applications =
            await tutorApplicationsRepository.GetAllWithDocumentsAndApplicantAndInterviewByOnboardingStep(
                onboardingStep,
                cancellationToken
            );

        logger.LogInformation(
            "{Service} Found {Count} tutor application(s).",
            ServiceName,
            applications.Count
        );

        return Result.Ok(applications);
    }
}
