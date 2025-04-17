namespace Application.UseCases.Commands.TutorApplications.Step3.AfterInterview;

public static class ApproveInterview
{
    public static class Errors
    {
        public static class Handler
        {
            public static Error InvalidInterviewState =
                new(ErrorType.Validation, "Interview must be confirmed before approval.");
        }
    }

    public record Command(Guid TutorApplicationId) : IRequest<Result>;

    public class Handler(
        ITutorApplicationsRepository tutorApplicationsRepository,
        IEmailService emailService,
        ILogger<Handler> logger
    ) : IRequestHandler<Command, Result>
    {
        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            var application = await tutorApplicationsRepository.GetByIdAsync(request.TutorApplicationId, cancellationToken);
            if (application?.Interview?.Status != TutorApplicationInterview.InterviewStatus.Confirmed)
            {
                return Result.Fail(Errors.Handler.InvalidInterviewState);
            }

            application.CurrentStep = TutorApplication.OnboardingStep.DocumentsRequested;

            var email = new EmailPayload<TutorRequestDocumentsEmail>(
                application.Applicant.EmailAddress,
                new(application.Applicant.FullName)
            );

            var emailResult = await emailService.SendEmailAsync(email, cancellationToken);
            if (emailResult.IsFailure)
            {
                logger.LogError("[ApproveInterview] Failed to send email: {@Errors}", emailResult.Errors);
                return Result.Fail(emailResult.Errors);
            }

            logger.LogInformation("[ApproveInterview] TutorApplication {Id} approved and document step started.", application.Id);

            await tutorApplicationsRepository.SaveChangesAsync(cancellationToken);

            return Result.Ok();
        }
    }
}