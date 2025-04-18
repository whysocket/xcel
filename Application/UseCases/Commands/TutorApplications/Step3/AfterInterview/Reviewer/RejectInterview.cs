using Xcel.Services.Auth.Interfaces.Services;

namespace Application.UseCases.Commands.TutorApplications.Step3.AfterInterview.Reviewer;

public static class RejectInterview
{
    public static class Errors
    {
        public static class Handler
        {
            public static Error InvalidInterviewState =
                new(ErrorType.Validation, "Interview must be confirmed before rejection.");

            public static Error TutorApplicationNotFound =
                new(ErrorType.NotFound, "Tutor application not found.");
        }
    }

    public record Command(Guid TutorApplicationId, string? RejectionReason = null) : IRequest<Result>;

    public class Handler(
        ITutorApplicationsRepository tutorApplicationsRepository,
        IUserService userService,
        IEmailService emailService,
        ILogger<Handler> logger
    ) : IRequestHandler<Command, Result>
    {
        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            var application =
                await tutorApplicationsRepository.GetByIdAsync(request.TutorApplicationId, cancellationToken);
            if (application == null)
            {
                logger.LogWarning("[RejectInterview] TutorApplication not found: {Id}", request.TutorApplicationId);
                return Result.Fail(Errors.Handler.TutorApplicationNotFound);
            }

            if (application.Interview?.Status != TutorApplicationInterview.InterviewStatus.Confirmed)
            {
                logger.LogWarning("[RejectInterview] Cannot reject unless interview is confirmed. Current: {Status}",
                    application.Interview?.Status);
                return Result.Fail(Errors.Handler.InvalidInterviewState);
            }

            var rejectionEmail = new EmailPayload<TutorInterviewRejectionEmail>(
                application.Applicant.EmailAddress,
                new(application.Applicant.FullName, request.RejectionReason)
            );

            var emailResult = await emailService.SendEmailAsync(rejectionEmail, cancellationToken);
            if (emailResult.IsFailure)
            {
                logger.LogError("[RejectInterview] Failed to send rejection email: {@Errors}", emailResult.Errors);
                return Result.Fail(emailResult.Errors);
            }

            logger.LogInformation("[RejectInterview] Rejection email sent to: {Email}",
                application.Applicant.EmailAddress);

            var deleteResult = await userService.DeleteAccountAsync(application.Applicant.Id, cancellationToken);
            if (deleteResult.IsFailure)
            {
                logger.LogError("[RejectInterview] Failed to delete applicant account: {Errors}", deleteResult.Errors);
                return Result.Fail(deleteResult.Errors);
            }

            application.IsRejected = true;
            tutorApplicationsRepository.Update(application);
            await tutorApplicationsRepository.SaveChangesAsync(cancellationToken);

            logger.LogInformation("[RejectInterview] TutorApplication {Id} rejected and account deleted.",
                application.Id);

            return Result.Ok();
        }
    }
}