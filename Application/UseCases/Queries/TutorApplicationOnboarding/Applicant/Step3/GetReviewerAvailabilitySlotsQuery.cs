using Application.UseCases.Queries.Availability;

namespace Application.UseCases.Queries.TutorApplicationOnboarding.Applicant.Step3;

/// <summary>
/// Allows an applicant to fetch the available interview time slots for their assigned reviewer.
/// </summary>
public interface IGetReviewerAvailabilitySlotsQuery
{
    Task<Result<List<TimeSlot>>> ExecuteAsync(Guid applicantUserId, Guid tutorApplicationId, DateOnly date, CancellationToken cancellationToken = default);
}

public record TimeSlot(DateTime StartUtc, DateTime EndUtc);

internal sealed class GetReviewerAvailabilitySlotsQuery(
    ITutorApplicationsRepository tutorApplicationsRepository,
    IGetAvailabilitySlotsQuery availabilitySlotsQuery,
    TimeProvider timeProvider,
    ILogger<GetReviewerAvailabilitySlotsQuery> logger
) : IGetReviewerAvailabilitySlotsQuery
{
    private const string ServiceName = "[GetReviewerAvailabilitySlotsQuery]";

    public async Task<Result<List<TimeSlot>>> ExecuteAsync(Guid applicantUserId, Guid tutorApplicationId, DateOnly date, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("{Service} Fetching reviewer availability for applicant {ApplicantId} and application {ApplicationId}", ServiceName, applicantUserId, tutorApplicationId);

        var application = await tutorApplicationsRepository.GetByIdAsync(tutorApplicationId, cancellationToken);
        if (application is null)
        {
            logger.LogWarning("{Service} Tutor application not found for ID {ApplicationId}", ServiceName, tutorApplicationId);
            return Result.Fail<List<TimeSlot>>(new Error(ErrorType.Forbidden, "Tutor application not found."));
        }

        if (application.ApplicantId != applicantUserId)
        {
            logger.LogWarning("{Service} Applicant {ApplicantId} is not authorized to access application {ApplicationId}", ServiceName, applicantUserId, tutorApplicationId);
            return Result.Fail<List<TimeSlot>>(new Error(ErrorType.Forbidden, "You are not authorized to view this reviewer's availability."));
        }

        if (application.Interview?.ReviewerId is not { } reviewerId)
        {
            logger.LogWarning("{Service} No reviewer assigned for application {ApplicationId}", ServiceName, tutorApplicationId);
            return Result.Fail<List<TimeSlot>>(new Error(ErrorType.Forbidden, "No reviewer assigned yet."));
        }

        logger.LogInformation("{Service} Getting availability for reviewer {ReviewerId} on {Date}", ServiceName, reviewerId, date);

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var startOfDay = date.ToDateTime(TimeOnly.MinValue);
        var from = startOfDay < now ? now : startOfDay;
        var to = date.ToDateTime(TimeOnly.MaxValue);

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
            logger.LogError("{Service} Failed to fetch availability for reviewer {ReviewerId}. Errors: {@Errors}", ServiceName, reviewerId, result.Errors);
            return Result.Fail<List<TimeSlot>>(result.Errors);
        }

        var timeSlots = result.Value.ConvertAll(s => new TimeSlot(s.StartUtc, s.EndUtc));
        return Result.Ok(timeSlots);
    }
}