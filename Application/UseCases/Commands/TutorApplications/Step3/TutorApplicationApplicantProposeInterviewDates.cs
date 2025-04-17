namespace Application.UseCases.Commands.TutorApplications.Step3;

public static class TutorApplicationApplicantProposeInterviewDates
{
    public static class Errors
    {
        public static Error TutorApplicationNotFound =
            new(ErrorType.NotFound, "Tutor application or interview not found.");

        public static Error InterviewNotInCorrectState =
            new(ErrorType.Validation, "Interview is not ready for new proposed dates.");

        public static Error EmailSendFailed =
            new(ErrorType.Unexpected, "Failed to send email to reviewer.");
    }

    public record Command(Guid TutorApplicationId, List<DateTime> ProposedDates, string? Observations) : IRequest<Result>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator(TimeProvider timeProvider)
        {
            RuleFor(x => x.ProposedDates)
                .NotEmpty().WithMessage("At least one interview date must be proposed.")
                .Must(dates => dates.Count <= 3)
                .WithMessage("You can propose up to 3 interview dates.")
                .Must(dates => dates.All(d => d > timeProvider.GetUtcNow()))
                .WithMessage("All proposed dates must be in the future.");
        }
    }

    public class Handler(
        ITutorApplicationsRepository tutorApplicationsRepository,
        IEmailService emailService,
        ILogger<Handler> logger
    ) : IRequestHandler<Command, Result>
    {
        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            var tutorApplicationId = request.TutorApplicationId;
            var proposedDatesCount = request.ProposedDates.Count;

            logger.LogInformation(
                "[ProposeInterviewDates] Tutor {TutorApplicationId} proposing {Count} date(s)",
                tutorApplicationId,
                proposedDatesCount);

            var application = await tutorApplicationsRepository.GetByIdAsync(tutorApplicationId, cancellationToken);
            if (application?.Interview is null)
            {
                logger.LogWarning("[ProposeInterviewDates] Application or interview not found for ID: {TutorApplicationId}", tutorApplicationId);
                return Result.Fail(Errors.TutorApplicationNotFound);
            }

            var interview = application.Interview;

            if (interview.Status is not
                (TutorApplicationInterview.InterviewStatus.AwaitingReviewerProposedDates or
                 TutorApplicationInterview.InterviewStatus.AwaitingApplicantConfirmation))
            {
                logger.LogWarning("[ProposeInterviewDates] Invalid interview status: {Status} for TutorApplicationId: {TutorApplicationId}",
                    interview.Status, tutorApplicationId);

                return Result.Fail(Errors.InterviewNotInCorrectState);
            }

            interview.ProposedDates = request.ProposedDates;
            interview.Observations = request.Observations;
            interview.Status = TutorApplicationInterview.InterviewStatus.AwaitingReviewerConfirmation;

            tutorApplicationsRepository.Update(application);
            await tutorApplicationsRepository.SaveChangesAsync(cancellationToken);

            logger.LogInformation("[ProposeInterviewDates] Interview dates updated and status set to AwaitingReviewerConfirmation for {TutorApplicationId}",
                tutorApplicationId);

            var reviewer = interview.Reviewer;
            var reviewerEmail = reviewer.EmailAddress;

            var emailPayload = new EmailPayload<ReviewerInterviewDatesEmail>(
                reviewerEmail,
                new(application.Applicant.FullName, request.ProposedDates, request.Observations));

            var emailResult = await emailService.SendEmailAsync(emailPayload, cancellationToken);
            if (emailResult.IsFailure)
            {
                logger.LogError("[ProposeInterviewDates] Failed to send email to reviewer: {ReviewerEmail}, Errors: {@Errors}", reviewerEmail, emailResult.Errors);
                return Result.Fail(Errors.EmailSendFailed);
            }

            logger.LogInformation("[ProposeInterviewDates] Email sent to reviewer: {ReviewerEmail}", reviewerEmail);

            return Result.Ok();
        }
    }
}