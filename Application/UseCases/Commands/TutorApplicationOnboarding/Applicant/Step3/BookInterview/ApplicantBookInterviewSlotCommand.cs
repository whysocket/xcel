namespace Application.UseCases.Commands.TutorApplicationOnboarding.Applicant.Step3.BookInterview;

/// <summary>
/// Applicant books an interview by selecting a time slot based on the reviewer's availability.
/// </summary>
public interface IApplicantBookInterviewSlotCommand
{
    Task<Result> ExecuteAsync(
        ApplicantBookInterviewSlotInput input,
        CancellationToken cancellationToken = default
    );
}

public record ApplicantBookInterviewSlotInput(
    Guid TutorApplicationId,
    DateTime SelectedStartUtc,
    string? Observations
);

internal static class ApplicantBookInterviewSlotCommandErrors
{
    public static Error ApplicationOrInterviewNotFound =>
        new(ErrorType.NotFound, "Tutor application or interview not found.");

    public static Error InterviewNotSelectable =>
        new(ErrorType.Validation, "Interview is not in a state that allows slot selection.");

    public static Error InvalidSlot =>
        new(ErrorType.Validation, "Selected slot is not valid according to reviewer availability.");

    public static Error EmailSendFailed =>
        new(ErrorType.Unexpected, "Failed to send confirmation email to reviewer.");
}

internal sealed class ApplicantBookInterviewSlotCommand(
    ITutorApplicationsRepository tutorApplicationsRepository,
    IAvailabilityRulesRepository availabilityRepository,
    IEmailService emailService,
    ILogger<ApplicantBookInterviewSlotCommand> logger
) : IApplicantBookInterviewSlotCommand
{
    private const string ServiceName = "[ApplicantBookInterviewSlotCommand]";

    public async Task<Result> ExecuteAsync(
        ApplicantBookInterviewSlotInput input,
        CancellationToken cancellationToken = default
    )
    {
        var application = await tutorApplicationsRepository.GetByIdAsync(
            input.TutorApplicationId,
            cancellationToken
        );
        if (application?.Interview is null)
        {
            return Result.Fail(
                ApplicantBookInterviewSlotCommandErrors.ApplicationOrInterviewNotFound
            );
        }

        var interview = application.Interview;

        if (
            interview.Status
            != TutorApplicationInterview.InterviewStatus.AwaitingApplicantSlotSelection
        )
        {
            return Result.Fail(ApplicantBookInterviewSlotCommandErrors.InterviewNotSelectable);
        }

        var reviewerRules = await availabilityRepository.GetByOwnerAndDateAsync(
            interview.ReviewerId,
            input.SelectedStartUtc.Date,
            cancellationToken
        );

        var proposedSlot = new TimeRange(
            input.SelectedStartUtc,
            input.SelectedStartUtc.AddMinutes(30)
        );

        var isValid = reviewerRules.Any(rule =>
            !rule.IsExcluded
            && rule.DayOfWeek == input.SelectedStartUtc.DayOfWeek
            && input.SelectedStartUtc.Date >= rule.ActiveFromUtc.Date
            && (
                rule.ActiveUntilUtc == null
                || input.SelectedStartUtc.Date <= rule.ActiveUntilUtc.Value.Date
            )
            && proposedSlot.Start.TimeOfDay >= rule.StartTimeUtc
            && proposedSlot.End.TimeOfDay <= rule.EndTimeUtc
        );

        if (!isValid)
        {
            return Result.Fail(ApplicantBookInterviewSlotCommandErrors.InvalidSlot);
        }

        interview.ScheduledAtUtc = input.SelectedStartUtc;
        interview.Status = TutorApplicationInterview.InterviewStatus.Confirmed;
        interview.Observations = input.Observations;
        interview.ConfirmedBy = application.ApplicantId;

        var email = new EmailPayload<InterviewScheduledEmail>(
            [interview.Reviewer.EmailAddress, application.Applicant.EmailAddress],
            new(application.Applicant.FullName, interview.Reviewer.FullName, input.SelectedStartUtc)
        );

        var emailResult = await emailService.SendEmailAsync(email, cancellationToken);
        if (emailResult.IsFailure)
        {
            logger.LogError(
                "{Service} Failed to send interview confirmation email: {Errors}",
                ServiceName,
                emailResult.Errors
            );
            return Result.Fail(ApplicantBookInterviewSlotCommandErrors.EmailSendFailed);
        }

        tutorApplicationsRepository.Update(application);
        await tutorApplicationsRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "{Service} Applicant booked interview slot at {Time}",
            ServiceName,
            input.SelectedStartUtc
        );
        return Result.Ok();
    }

    private readonly record struct TimeRange(DateTime Start, DateTime End);
}
