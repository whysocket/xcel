namespace Application.UseCases.Commands.TutorApplicationOnboarding.Step3.BookInterview.Reviewer;

public static class ReviewerProposeInterviewDates
{
    public static class Errors
    {
        public static class Handler
        {
            public static Error TutorApplicationNotFound =
                new(ErrorType.NotFound, "Tutor application or interview not found.");

            public static Error InterviewNotInCorrectState =
                new(ErrorType.Validation, "Interview is not in a state for reviewer to propose new dates.");

            public static Error EmailSendFailed =
                new(ErrorType.Unexpected, "Failed to send email to tutor.");
        }
    }

    public record Command(Guid TutorApplicationId, List<DateTime> ProposedDates, string? Observations)
        : IRequest<Result>;

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
                "[ReviewerProposeInterviewDates] Reviewer proposing {Count} new date(s) for TutorApplicationId: {TutorApplicationId}",
                proposedDatesCount,
                tutorApplicationId);

            var application = await tutorApplicationsRepository.GetByIdAsync(tutorApplicationId, cancellationToken);
            if (application?.Interview is null)
            {
                logger.LogWarning(
                    "[ReviewerProposeInterviewDates] Application or interview not found for ID: {TutorApplicationId}",
                    tutorApplicationId);
                return Result.Fail(Errors.Handler.TutorApplicationNotFound);
            }

            var interview = application.Interview;
            if (interview.Status != TutorApplicationInterview.InterviewStatus.AwaitingReviewerConfirmation)
            {
                logger.LogWarning(
                    "[ReviewerProposeInterviewDates] Interview not awaiting reviewer input. Current status: {Status}",
                    interview.Status);
                return Result.Fail(Errors.Handler.InterviewNotInCorrectState);
            }

            interview.ProposedDates = request.ProposedDates;
            interview.Observations = request.Observations;
            interview.Status = TutorApplicationInterview.InterviewStatus.AwaitingApplicantConfirmation;

            var applicant = application.Applicant;
            var applicantEmail = applicant.EmailAddress;

            var emailPayload = new EmailPayload<TutorApplicantProposedDatesEmail>(
                applicantEmail,
                new(applicant.FullName, request.ProposedDates, request.Observations));

            var emailResult = await emailService.SendEmailAsync(emailPayload, cancellationToken);
            if (emailResult.IsFailure)
            {
                logger.LogError(
                    "[ReviewerProposeInterviewDates] Failed to send email to tutor: {TutorEmail}, Errors: {@Errors}",
                    applicantEmail, emailResult.Errors);
                return Result.Fail(Errors.Handler.EmailSendFailed);
            }

            logger.LogInformation("[ReviewerProposeInterviewDates] Email sent to tutor: {TutorEmail}", applicantEmail);

            tutorApplicationsRepository.Update(application);
            await tutorApplicationsRepository.SaveChangesAsync(cancellationToken);
            
            logger.LogInformation(
                "[ReviewerProposeInterviewDates] Interview updated with new dates and status AwaitingApplicantConfirmation for {TutorApplicationId}",
                tutorApplicationId);
            
            return Result.Ok();
        }
    }
}