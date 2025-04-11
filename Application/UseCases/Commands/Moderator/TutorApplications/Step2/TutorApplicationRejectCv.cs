using Xcel.Services.Auth.Interfaces.Services;
using Xcel.Services.Email.Interfaces;
using Xcel.Services.Email.Models;
using Xcel.Services.Email.Templates.TutorRejectionEmail;
using Microsoft.Extensions.Logging;

namespace Application.UseCases.Commands.Moderator.TutorApplications.Step2;

public static class TutorApplicationRejectCv
{
    public record Command(Guid TutorApplicationId, string? RejectionReason = null) : IRequest<Result>;

    public class Handler(
        ITutorApplicationsRepository tutorApplicationsRepository,
        IUserService userService,
        IEmailSender emailSender,
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

            if (tutorApplication.IsRejected)
            {
                logger.LogWarning("[TutorApplicationRejectCv] Tutor Application with ID '{TutorApplicationId}' is already rejected.", request.TutorApplicationId);
                return Result.Fail(new Error(ErrorType.Conflict, $"Tutor Application with ID '{request.TutorApplicationId}' is already rejected."));
            }

            if (tutorApplication.CurrentStep != TutorApplication.OnboardingStep.CvUnderReview)
            {
                logger.LogError("[TutorApplicationRejectCv] Tutor Application with ID '{TutorApplicationId}' is not in the CV review state. Current step: {CurrentStep}", request.TutorApplicationId, tutorApplication.CurrentStep);
                return Result.Fail(new Error(ErrorType.Validation, $"Tutor Application with ID '{request.TutorApplicationId}' is not in the CV review state."));
            }

            if (tutorApplication.Documents.Count != 1)
            {
                logger.LogError("[TutorApplicationRejectCv] Tutor Application with ID '{TutorApplicationId}' has incorrect document count: {DocumentCount}", request.TutorApplicationId, tutorApplication.Documents.Count);
                return Result.Fail(new Error(ErrorType.Validation, $"Tutor Application with ID '{request.TutorApplicationId}' has an incorrect number of submitted documents."));
            }

            var emailPayload = new EmailPayload<TutorRejectionEmailData>(
                "Your application was rejected",
                tutorApplication.Person.EmailAddress,
                new TutorRejectionEmailData(tutorApplication.Person.FirstName, tutorApplication.Person.LastName,
                    request.RejectionReason));

            try
            {
                await emailSender.SendEmailAsync(emailPayload, cancellationToken);
                logger.LogInformation("[TutorApplicationRejectCv] Rejection email sent to: {Email}", tutorApplication.Person.EmailAddress);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[TutorApplicationRejectCv] Failed to send rejection email to: {Email}", tutorApplication.Person.EmailAddress);
            }

            var deleteAccountResult = await userService.DeleteAccountAsync(tutorApplication.Person.Id, cancellationToken);
            if (deleteAccountResult.IsFailure)
            {
                logger.LogError("[TutorApplicationRejectCv] Failed to delete account for PersonId: {PersonId}, Errors: {@Errors}", tutorApplication.Person.Id, deleteAccountResult.Errors);
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