namespace Application.UseCases.Commands.TutorApplicationOnboarding.Applicant.Step3.BookInterview.Reviewer;

/// <summary>
/// AfterInterview requests a new slot from the applicant after an interview has been confirmed.
/// Sets status back to AwaitingApplicantSlotSelection and notifies the applicant.
/// </summary>
public interface IReviewerRequestInterviewRescheduleCommand
{
    Task<Result> ExecuteAsync(ReviewerRequestInterviewRescheduleInput input, CancellationToken cancellationToken = default);
}

public record ReviewerRequestInterviewRescheduleInput(
    Guid TutorApplicationId,
    string? Reason // optional message for applicant
);

internal static class ReviewerRequestInterviewRescheduleCommandErrors
{
    public static Error ApplicationOrInterviewNotFound =>
        new(ErrorType.NotFound, "Tutor application or interview not found.");

    public static Error InterviewNotConfirmed =>
        new(ErrorType.Validation, "Interview must be confirmed before requesting a reschedule.");

    public static Error EmailSendFailed =>
        new(ErrorType.Unexpected, "Failed to send reschedule email to applicant.");
}

internal sealed class ReviewerRequestInterviewRescheduleCommand(
    ITutorApplicationsRepository tutorApplicationsRepository,
    IEmailService emailService,
    ILogger<ReviewerRequestInterviewRescheduleCommand> logger
) : IReviewerRequestInterviewRescheduleCommand
{
    private const string ServiceName = "[ReviewerRequestInterviewRescheduleCommand]";

    public async Task<Result> ExecuteAsync(ReviewerRequestInterviewRescheduleInput input, CancellationToken cancellationToken = default)
    {
        var application = await tutorApplicationsRepository.GetByIdAsync(input.TutorApplicationId, cancellationToken);
        if (application?.Interview is null)
        {
            return Result.Fail(ReviewerRequestInterviewRescheduleCommandErrors.ApplicationOrInterviewNotFound);
        }

        var interview = application.Interview;

        if (interview.Status != TutorApplicationInterview.InterviewStatus.Confirmed)
        {
            return Result.Fail(ReviewerRequestInterviewRescheduleCommandErrors.InterviewNotConfirmed);
        }

        // Reset interview status
        interview.Status = TutorApplicationInterview.InterviewStatus.AwaitingApplicantSlotSelection;
        interview.Observations = input.Reason;
        interview.ScheduledAtUtc = null;

        var email = new EmailPayload<ReviewerRescheduleRequestEmail>(
            application.Applicant.EmailAddress,
            new(application.Applicant.FullName, interview.Reviewer.FullName, input.Reason));

        var emailResult = await emailService.SendEmailAsync(email, cancellationToken);
        if (emailResult.IsFailure)
        {
            logger.LogError("{Service} Failed to send email: {Errors}", ServiceName, emailResult.Errors);
            return Result.Fail(ReviewerRequestInterviewRescheduleCommandErrors.EmailSendFailed);
        }

        tutorApplicationsRepository.Update(application);
        await tutorApplicationsRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation("{Service} Interview status reset to AwaitingApplicantSlotSelection for application {Id}", ServiceName, application.Id);
        return Result.Ok();
    }
}
