using Xcel.Services.Auth.Public;

namespace Application.UseCases.Commands.TutorApplicationOnboarding.Moderator.Step2;

public static class ApplicationRejectCv
{
    public record Command(Guid TutorApplicationId, string? RejectionReason = null) : IRequest<Result>;

    public class Handler(
        ITutorApplicationsRepository tutorApplicationsRepository,
        IAuthServiceSdk authServiceSdk,
        IEmailService emailService,
        ILogger<Handler> logger) : IRequestHandler<Command, Result>
    {
        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            logger.LogInformation(
                "[ApplicationRejectCv] Attempting to reject CV for TutorApplicationId: {TutorApplicationId}",
                request.TutorApplicationId);

            var tutorApplication =
                await tutorApplicationsRepository.GetByIdAsync(request.TutorApplicationId, cancellationToken);
            if (tutorApplication == null)
            {
                logger.LogError(
                    "[ApplicationRejectCv] Tutor Application with ID '{TutorApplicationId}' not found.",
                    request.TutorApplicationId);
                return Result.Fail(new Error(ErrorType.NotFound,
                    $"Tutor Application with ID '{request.TutorApplicationId}' not found."));
            }

            var validationResult = tutorApplication.ValidateTutorApplicationForCvReview(logger);
            if (validationResult.IsFailure)
            {
                return validationResult;
            }

            var emailPayload = new EmailPayload<ApplicantCvRejectionEmail>(
                tutorApplication.Applicant.EmailAddress,
                new(tutorApplication.Applicant.FullName, request.RejectionReason));

            var emailResult = await emailService.SendEmailAsync(emailPayload, cancellationToken);

            if (emailResult.IsFailure)
            {
                logger.LogError("[ApplicationRejectCv] Failed to send rejection email to: {Email}",
                    tutorApplication.Applicant.EmailAddress);
                return Result.Fail(emailResult.Errors);
            }

            logger.LogInformation("[ApplicationRejectCv] Rejection email sent to: {Email}",
                tutorApplication.Applicant.EmailAddress);

            var deleteAccountResult =
                await authServiceSdk.DeleteAccountAsync(tutorApplication.Applicant.Id, cancellationToken);
            if (deleteAccountResult.IsFailure)
            {
                logger.LogError(
                    "[ApplicationRejectCv] Failed to delete account for ApplicantId: {ApplicantId}, Errors: {@Errors}",
                    tutorApplication.Applicant.Id, deleteAccountResult.Errors);
                return Result.Fail(deleteAccountResult.Errors);
            }

            tutorApplication.IsRejected = true;
            tutorApplicationsRepository.Update(tutorApplication);
            await tutorApplicationsRepository.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "[ApplicationRejectCv] Tutor Application rejected for TutorApplicationId: {TutorApplicationId}",
                request.TutorApplicationId);

            return Result.Ok();
        }
    }
}