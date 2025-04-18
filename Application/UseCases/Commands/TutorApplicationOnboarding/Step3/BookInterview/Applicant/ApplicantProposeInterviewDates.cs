namespace Application.UseCases.Commands.TutorApplicationOnboarding.Step3.BookInterview.Applicant;

public static class ApplicantProposeInterviewDates
{
    public static class Errors
    {
        public static class Commnad
        {
            public static string AtLeastOneProposeDateIsRequired = "At least one interview date must be proposed.";
            public static string ProposeUpToThreeDates = "You can propose up to 3 interview dates.";
            public static string AllProposedDatesInFuture = "All proposed dates must be in the future.";
        }

        public static class Handler
        {
            public static Error TutorApplicationNotFound =
                new(ErrorType.NotFound, "Tutor application or interview not found.");

            public static Error InterviewNotInCorrectState =
                new(ErrorType.Validation, "Interview is not ready for new proposed dates.");

            public static Error EmailSendFailed =
                new(ErrorType.Unexpected, "Failed to send email to reviewer.");
        }
    }

    public record Command(Guid TutorApplicationId, List<DateTime> ProposedDates, string? Observations)
        : IRequest<Result>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator(TimeProvider timeProvider)
        {
            RuleFor(x => x.ProposedDates)
                .NotEmpty().WithMessage(Errors.Commnad.AtLeastOneProposeDateIsRequired)
                .Must(dates => dates.Count <= 3)
                .WithMessage(Errors.Commnad.ProposeUpToThreeDates)
                .Must(dates => dates.All(d => d > timeProvider.GetUtcNow()))
                .WithMessage(Errors.Commnad.AllProposedDatesInFuture);
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
                "[ApplicantProposeInterviewDates] Tutor {TutorApplicationId} proposing {Count} date(s)",
                tutorApplicationId,
                proposedDatesCount);

            var application = await tutorApplicationsRepository.GetByIdAsync(tutorApplicationId, cancellationToken);
            if (application?.Interview is null)
            {
                logger.LogWarning(
                    "[ApplicantProposeInterviewDates] Application or interview not found for ID: {TutorApplicationId}",
                    tutorApplicationId);
                return Result.Fail(Errors.Handler.TutorApplicationNotFound);
            }

            var interview = application.Interview;

            if (interview.Status is not
                (TutorApplicationInterview.InterviewStatus.AwaitingReviewerProposedDates or
                TutorApplicationInterview.InterviewStatus.AwaitingApplicantConfirmation))
            {
                logger.LogWarning(
                    "[ApplicantProposeInterviewDates] Invalid interview status: {Status} for TutorApplicationId: {TutorApplicationId}",
                    interview.Status, tutorApplicationId);

                return Result.Fail(Errors.Handler.InterviewNotInCorrectState);
            }

            interview.ProposedDates = request.ProposedDates;
            interview.Observations = request.Observations;
            interview.Status = TutorApplicationInterview.InterviewStatus.AwaitingReviewerConfirmation;

            var reviewer = interview.Reviewer;
            var reviewerEmail = reviewer.EmailAddress;

            var emailPayload = new EmailPayload<ReviewerInterviewDatesEmail>(
                reviewerEmail,
                new(application.Applicant.FullName, request.ProposedDates, request.Observations));

            var emailResult = await emailService.SendEmailAsync(emailPayload, cancellationToken);
            if (emailResult.IsFailure)
            {
                logger.LogError(
                    "[ApplicantProposeInterviewDates] Failed to send email to reviewer: {ReviewerEmail}, Errors: {@Errors}",
                    reviewerEmail, emailResult.Errors);
                return Result.Fail(Errors.Handler.EmailSendFailed);
            }

            logger.LogInformation("[ApplicantProposeInterviewDates] Email sent to reviewer: {ReviewerEmail}", reviewerEmail);

            tutorApplicationsRepository.Update(application);
            await tutorApplicationsRepository.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "[ApplicantProposeInterviewDates] Interview dates updated and status set to AwaitingReviewerConfirmation for {TutorApplicationId}",
                tutorApplicationId);

            return Result.Ok();
        }
    }
}