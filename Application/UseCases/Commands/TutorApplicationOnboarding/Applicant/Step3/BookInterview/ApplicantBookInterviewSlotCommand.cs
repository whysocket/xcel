using Xcel.Services.Auth.Interfaces.Services;

namespace Application.UseCases.Commands.TutorApplicationOnboarding.Applicant.Step3.BookInterview;

/// <summary>
/// Applicant books an interview by selecting a time slot based on the reviewer's availability.
/// Validates the selected slot against the reviewer's AvailabilityStandard, AvailabilityOneOff, FullDay Exclusion, and TimeBased Exclusion rules.
/// Includes a check to prevent double-booking the slot.
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
        new(
            ErrorType.Validation,
            "Selected slot is not valid according to reviewer availability or conflicts with an exclusion."
        );

    public static Error SlotAlreadyBooked =>
        new(ErrorType.Conflict, "The selected time slot has already been booked.");

    public static Error EmailSendFailed =>
        new(ErrorType.Unexpected, "Failed to send confirmation email to reviewer.");
}

internal sealed class ApplicantBookInterviewSlotCommand(
    ITutorApplicationsRepository tutorApplicationsRepository,
    IAvailabilityRulesRepository availabilityRepository,
    IEmailService emailService,
    ILogger<ApplicantBookInterviewSlotCommand> logger,
    IClientInfoService clientInfoService
) : IApplicantBookInterviewSlotCommand
{
    private const string ServiceName = "[ApplicantBookInterviewSlotCommand]";

    public async Task<Result> ExecuteAsync(
        ApplicantBookInterviewSlotInput input,
        CancellationToken cancellationToken = default
    )
    {
        var applicantId = clientInfoService.UserId;

        logger.LogInformation(
            "{Service} Applicant {ApplicantId} attempting to book interview slot for application {ApplicationId} at {Start:yyyy-MM-dd HH:mm}",
            ServiceName,
            applicantId,
            input.TutorApplicationId,
            input.SelectedStartUtc
        );

        var application = await tutorApplicationsRepository.GetByIdAsync(
            input.TutorApplicationId,
            cancellationToken
        );
        if (application?.Interview is null)
        {
            logger.LogWarning(
                "{Service} Application or Interview not found for ID {ApplicationId}",
                ServiceName,
                input.TutorApplicationId
            );
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
            logger.LogWarning(
                "{Service} Interview {InterviewId} status is {Status}, not AwaitingApplicantSlotSelection.",
                ServiceName,
                interview.Id,
                interview.Status
            );
            return Result.Fail(ApplicantBookInterviewSlotCommandErrors.InterviewNotSelectable);
        }

        // --- Check for Double Booking ---
        var proposedSlot = new TimeRange(
            input.SelectedStartUtc,
            input.SelectedStartUtc.AddMinutes(30) // Assuming 30-minute slots
        );

        var existingBooking = await tutorApplicationsRepository.GetBookingAtSlotAsync(
            interview.ReviewerId,
            proposedSlot.Start,
            proposedSlot.End,
            cancellationToken
        );

        if (existingBooking != null)
        {
            logger.LogWarning(
                "{Service} Proposed slot {Start:yyyy-MM-dd HH:mm} for Reviewer {OwnerId} already booked by Interview {BookingId}.",
                ServiceName,
                proposedSlot.Start,
                interview.ReviewerId,
                existingBooking.Id
            );
            return Result.Fail(ApplicantBookInterviewSlotCommandErrors.SlotAlreadyBooked);
        }
        // --- End Double Booking Check ---

        // --- Validation against availability rules ---
        // Fetch all availability and exclusion rules active on the date of the proposed slot.
        // GetRulesActiveOnDateAsync fetches ALL rule types active on the date.
        var reviewerRules = await availabilityRepository.GetRulesActiveOnDateAsync( // Renamed call
            interview.ReviewerId,
            input.SelectedStartUtc.Date,
            cancellationToken
        );

        // Check if the proposed slot falls within *any* available time block (Standard or OneOff) active on this date
        bool fallsWithinAvailability = reviewerRules.Any(rule =>
            (
                rule.RuleType == AvailabilityRuleType.AvailabilityStandard
                || rule.RuleType == AvailabilityRuleType.AvailabilityOneOff
            )
            && rule.DayOfWeek == input.SelectedStartUtc.DayOfWeek // Check if rule applies to this day of week
            && input.SelectedStartUtc.Date >= rule.ActiveFromUtc.Date // Check if slot date is within rule's active date range
            && (
                rule.ActiveUntilUtc == null
                || input.SelectedStartUtc.Date <= rule.ActiveUntilUtc.Value.Date
            )
            && proposedSlot.Start.TimeOfDay >= rule.StartTimeUtc // Check if slot time is within rule's time range
            && proposedSlot.End.TimeOfDay <= rule.EndTimeUtc
        );

        // Check if the proposed slot overlaps with *any* exclusion (FullDay or TimeBased) active on this date
        bool overlapsWithExclusion = reviewerRules.Any(rule =>
            (
                rule.RuleType == AvailabilityRuleType.ExclusionFullDay
                || rule.RuleType == AvailabilityRuleType.ExclusionTimeBased
            )
            && rule.DayOfWeek == input.SelectedStartUtc.DayOfWeek // Check if rule applies to this day of week
            && input.SelectedStartUtc.Date >= rule.ActiveFromUtc.Date // Check if slot date is within rule's active date range
            && (
                rule.ActiveUntilUtc == null
                || input.SelectedStartUtc.Date <= rule.ActiveUntilUtc.Value.Date
            )
            // Check if the proposed slot's time range overlaps with the exclusion rule's time range
            && TimesOverlap(
                rule.StartTimeUtc,
                rule.EndTimeUtc,
                proposedSlot.Start.TimeOfDay,
                proposedSlot.End.TimeOfDay
            )
        );

        // The slot is valid only if it falls within *some* availability AND does NOT overlap with *any* exclusion
        bool isValidAccordingToRules = fallsWithinAvailability && !overlapsWithExclusion;

        if (!isValidAccordingToRules)
        {
            logger.LogWarning(
                "{Service} Proposed slot {Start:yyyy-MM-dd HH:mm} for {OwnerType} {OwnerId} on {Date:yyyy-MM-dd} is invalid according to rules. Reason: Falls within availability: {FallsWithin}, Overlaps exclusion: {OverlapsExclusion}.",
                ServiceName,
                input.SelectedStartUtc,
                AvailabilityOwnerType.Reviewer,
                interview.ReviewerId,
                input.SelectedStartUtc.Date,
                fallsWithinAvailability,
                overlapsWithExclusion
            );
            return Result.Fail(ApplicantBookInterviewSlotCommandErrors.InvalidSlot);
        }

        // --- Slot is valid according to rules and not double-booked, proceed with booking ---

        interview.ScheduledAtUtc = input.SelectedStartUtc;
        interview.Status = TutorApplicationInterview.InterviewStatus.Confirmed;
        interview.Observations = input.Observations;
        interview.ConfirmedBy = application.ApplicantId; // Or the user ID of the applicant if different from Person ID

        // Assuming EmailPayload and InterviewScheduledEmail DTO exist and are correct
        var email = new EmailPayload<InterviewScheduledEmail>(
            [interview.Reviewer.EmailAddress, application.Applicant.EmailAddress],
            new(application.Applicant.FullName, interview.Reviewer.FullName, input.SelectedStartUtc)
        );

        var emailResult = await emailService.SendEmailAsync(email, cancellationToken);
        if (emailResult.IsFailure)
        {
            logger.LogError(
                "{Service} Failed to send interview confirmation email for application {ApplicationId}. Errors: {@Errors}",
                ServiceName,
                application.Id,
                emailResult.Errors
            );
            // Decide whether to return failure here or allow booking and log email error
            // Returning failure as email confirmation seems critical.
            return Result.Fail(ApplicantBookInterviewSlotCommandErrors.EmailSendFailed);
        }

        tutorApplicationsRepository.Update(application);
        await tutorApplicationsRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "{Service} Applicant {ApplicantId} successfully booked interview slot at {Time:yyyy-MM-dd HH:mm} for application {ApplicationId}",
            ServiceName,
            applicantId, // Placeholder
            input.SelectedStartUtc,
            application.Id
        );
        return Result.Ok();
    }

    /// <summary>
    /// Represents a time range with start and end DateTime.
    /// </summary>
    private readonly record struct TimeRange(DateTime Start, DateTime End);

    /// <summary>
    /// Checks if two time ranges (TimeSpan) overlap. Assumes EndTime is exclusive.
    /// </summary>
    private static bool TimesOverlap(
        TimeSpan existingStart,
        TimeSpan existingEnd,
        TimeSpan newStart,
        TimeSpan newEnd
    )
    {
        return newStart < existingEnd && existingStart < newEnd;
    }
}
