using Microsoft.Extensions.Logging;
using Xcel.Services.Email.Interfaces;
using Xcel.Services.Email.Models;
using Xcel.Services.Email.Templates.TutorApprovalEmail;

namespace Application.UseCases.Commands.Moderator.TutorApplications.Step2;

public static class TutorApplicationApproveCv
{
    public record Command(Guid TutorApplicationId) : IRequest<Result>;

    public class Handler(
        ITutorApplicationsRepository tutorApplicationsRepository,
        IEmailSender emailSender,
        ILogger<Handler> logger) : IRequestHandler<Command, Result>
    {
        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            logger.LogInformation("[TutorApplicationApproveCv] Attempting to approve CV review for TutorApplicationId: {TutorApplicationId}", request.TutorApplicationId);

            var tutorApplication = await tutorApplicationsRepository.GetByIdAsync(request.TutorApplicationId, cancellationToken);
            if (tutorApplication == null)
            {
                logger.LogError("[TutorApplicationApproveCv] Tutor Application with ID '{TutorApplicationId}' not found.", request.TutorApplicationId);
                return Result.Fail(new Error(ErrorType.NotFound, $"Tutor Application with ID '{request.TutorApplicationId}' not found."));
            }

            if (tutorApplication.IsRejected)
            {
                logger.LogWarning("[TutorApplicationApproveCv] Tutor Application with ID '{TutorApplicationId}' is already rejected.", request.TutorApplicationId);
                return Result.Fail(new Error(ErrorType.Conflict, $"Tutor Application with ID '{request.TutorApplicationId}' is already rejected."));
            }

            if (tutorApplication.CurrentStep != TutorApplication.OnboardingStep.CvUnderReview)
            {
                logger.LogError("[TutorApplicationApproveCv] Tutor Application with ID '{TutorApplicationId}' is not in the CV review state. Current step: {CurrentStep}", request.TutorApplicationId, tutorApplication.CurrentStep);
                return Result.Fail(new Error(ErrorType.Validation, $"Tutor Application with ID '{request.TutorApplicationId}' is not in the CV review state."));
            }

            if (tutorApplication.Documents.Count != 1)
            {
                logger.LogError("[TutorApplicationApproveCv] Tutor Application with ID '{TutorApplicationId}' has incorrect document count: {DocumentCount}", request.TutorApplicationId, tutorApplication.Documents.Count);
                return Result.Fail(new Error(ErrorType.Validation, $"Tutor Application with ID '{request.TutorApplicationId}' has an incorrect number of submitted documents."));
            }

            var cvDocument = tutorApplication.Documents.SingleOrDefault(d => d.DocumentType == TutorDocument.TutorDocumentType.Cv);
            if (cvDocument is null || cvDocument.Status != TutorDocument.TutorDocumentStatus.Pending)
            {
                logger.LogError("[TutorApplicationApproveCv] Tutor Application with ID '{TutorApplicationId}' CV document is missing or not in pending state. CV document: {@CvDocument}", request.TutorApplicationId, cvDocument);
                return Result.Fail(new Error(ErrorType.Validation, $"Tutor Application with ID '{request.TutorApplicationId}' CV document is missing or not in pending state."));
            }

            tutorApplication.CurrentStep = TutorApplication.OnboardingStep.AwaitingInterviewBooking;

            tutorApplicationsRepository.Update(tutorApplication);
            await tutorApplicationsRepository.SaveChangesAsync(cancellationToken);

            logger.LogInformation("[TutorApplicationApproveCv] Tutor application updated for TutorApplicationId: {TutorApplicationId}", request.TutorApplicationId);

            var emailPayload = new EmailPayload<TutorApprovalEmailData>(
                "Your CV has been approved. Let’s book your interview",
                tutorApplication.Person.EmailAddress,
                new TutorApprovalEmailData(tutorApplication.Person.FirstName, tutorApplication.Person.LastName));

            try
            {
                await emailSender.SendEmailAsync(emailPayload, cancellationToken);
                logger.LogInformation("[TutorApplicationApproveCv] Approval email sent to: {Email}", tutorApplication.Person.EmailAddress);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[TutorApplicationApproveCv] Failed to send approval email to: {Email}", tutorApplication.Person.EmailAddress);
            }

            return Result.Ok();
        }
    }
}