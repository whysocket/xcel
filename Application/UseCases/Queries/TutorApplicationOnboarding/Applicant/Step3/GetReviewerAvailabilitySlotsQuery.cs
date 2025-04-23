using Application.UseCases.Queries.Availability;

namespace Application.UseCases.Queries.TutorApplicationOnboarding.Applicant.Step3;

/// <summary>
/// Allows an applicant to fetch the available interview time slots for their assigned reviewer.
/// </summary>
public interface IGetReviewerAvailabilitySlotsQuery
{
    Task<Result<List<TimeSlot>>> ExecuteAsync(
        Guid applicantUserId, 
        DateOnly dateUtc,
        CancellationToken cancellationToken = default);
}

public record TimeSlot(DateTime StartUtc, DateTime EndUtc);

internal static class GetReviewerAvailabilitySlotsQueryErrors
{
    public static Error TutorApplicationNotFound => new(ErrorType.Forbidden, $"Tutor application not found");

    public static Error Unauthorized(Guid applicantId, Guid applicationId) =>
        new(ErrorType.Forbidden,
            $"Applicant '{applicantId}' is not authorized to access application '{applicationId}'.");

    public static Error ReviewerNotAssigned(Guid applicationId) =>
        new(ErrorType.Forbidden, $"No reviewer assigned yet for application '{applicationId}'.");
}

internal sealed class GetReviewerAvailabilitySlotsQuery(
    ITutorApplicationsRepository tutorApplicationsRepository,
    IGetAvailabilitySlotsQuery availabilitySlotsQuery,
    TimeProvider timeProvider,
    ILogger<GetReviewerAvailabilitySlotsQuery> logger
) : IGetReviewerAvailabilitySlotsQuery
{
    private const string ServiceName = "[GetReviewerAvailabilitySlotsQuery]";

    public async Task<Result<List<TimeSlot>>> ExecuteAsync(Guid applicantUserId, DateOnly dateUtc,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("{Service} Fetching reviewer availability for applicant {ApplicantId}", ServiceName,
            applicantUserId);

        var application = await tutorApplicationsRepository.GetByUserIdAsync(applicantUserId, cancellationToken);
        if (application is null)
        {
            logger.LogWarning("{Service} Tutor application not found for ID user {applicantUserId}", ServiceName,
                applicantUserId);
            return Result.Fail<List<TimeSlot>>(GetReviewerAvailabilitySlotsQueryErrors.TutorApplicationNotFound);
        }

        if (application.ApplicantId != applicantUserId)
        {
            logger.LogWarning(
                "{Service} Applicant {ApplicantId} is not authorized to access application {ApplicationId}",
                ServiceName, applicantUserId, application.Id);
            return Result.Fail<List<TimeSlot>>(
                GetReviewerAvailabilitySlotsQueryErrors.Unauthorized(applicantUserId, application.Id));
        }

        if (application.Interview?.ReviewerId is not { } reviewerId)
        {
            logger.LogWarning("{Service} No reviewer assigned for application {ApplicationId}", ServiceName,
                application.Id);
            return Result.Fail<List<TimeSlot>>(
                GetReviewerAvailabilitySlotsQueryErrors.ReviewerNotAssigned(application.Id));
        }

        logger.LogInformation("{Service} Getting availability for reviewer {ReviewerId} on {Date}", ServiceName,
            reviewerId, dateUtc);

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var startOfDay = dateUtc.ToDateTime(TimeOnly.MinValue);
        var from = startOfDay < now ? now : startOfDay;
        var to = dateUtc.ToDateTime(TimeOnly.MaxValue);

        var result = await availabilitySlotsQuery.ExecuteAsync(
            new AvailabilitySlotsQueryInput(
                OwnerId: reviewerId,
                OwnerType: AvailabilityOwnerType.Reviewer,
                FromUtc: from,
                ToUtc: to,
                SlotDuration: TimeSpan.FromMinutes(30)),
            cancellationToken);

        if (result.IsFailure)
        {
            logger.LogError("{Service} Failed to fetch availability for reviewer {ReviewerId}. Errors: {@Errors}",
                ServiceName, reviewerId, result.Errors);
            return Result.Fail<List<TimeSlot>>(result.Errors);
        }

        var timeSlots = result.Value.ConvertAll(s => new TimeSlot(s.StartUtc, s.EndUtc));
        return Result.Ok(timeSlots);
    }
}