using Xcel.Services.Auth.Interfaces.Services;

namespace Application.UseCases.Commands.TutorApplications.Step2;

public static class TutorApplicationRejectCv
{
    public record Command(Guid TutorApplicationId, string? RejectionReason = null) : IRequest<Result>;

    public class Handler(
        ITutorApplicationsRepository tutorApplicationsRepository,
        IUserService userService,
        IEmailService emailService,
        ILogger<Handler> logger) : IRequestHandler<Command, Result>
    {
        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            logger.LogInformation("[TutorApplicationRejectCv] Attempting to reject CV for TutorApplicationId: {TutorApplicationId}", request.TutorApplicationId);

            var tutorApplication = await tutorApplicationsRepository.GetByIdAsync(request.TutorApplicationId, cancellationToken);
            if (tutorApplication == null)
            {
                logger.LogError("[TutorApplicationRejectCv] Tutor Application with ID '{TutorApplicationId}' not found.", request.TutorApplicationId);
                return Result.Fail(new Error(ErrorType.NotFound, $"Tutor Application with ID '{request.TutorApplicationId}' not found."));
            }

            var validationResult = tutorApplication.ValidateTutorApplicationForCvReview(logger);
            if (validationResult.IsFailure)
            {
                return validationResult;
            }

            var emailPayload = new EmailPayload<TutorCvRejectionEmail>(
                tutorApplication.Applicant.EmailAddress,
                new TutorCvRejectionEmail(
                    tutorApplication.Applicant.FullName,
                    request.RejectionReason));

            try
            {
                await emailService.SendEmailAsync(emailPayload, cancellationToken);
                logger.LogInformation("[TutorApplicationRejectCv] Rejection email sent to: {Email}", tutorApplication.Applicant.EmailAddress);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[TutorApplicationRejectCv] Failed to send rejection email to: {Email}", tutorApplication.Applicant.EmailAddress);
            }

            var deleteAccountResult = await userService.DeleteAccountAsync(tutorApplication.Applicant.Id, cancellationToken);
            if (deleteAccountResult.IsFailure)
            {
                logger.LogError("[TutorApplicationRejectCv] Failed to delete account for ApplicantId: {ApplicantId}, Errors: {@Errors}", tutorApplication.Applicant.Id, deleteAccountResult.Errors);
                return Result.Fail(deleteAccountResult.Errors);
            }

            tutorApplication.IsRejected = true;
            tutorApplicationsRepository.Update(tutorApplication);
            await tutorApplicationsRepository.SaveChangesAsync(cancellationToken);

            logger.LogInformation("[TutorApplicationRejectCv] Tutor Application rejected for TutorApplicationId: {TutorApplicationId}", request.TutorApplicationId);

            return Result.Ok();
        }
    }
}