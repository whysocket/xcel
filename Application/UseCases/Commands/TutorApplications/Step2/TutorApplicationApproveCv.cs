using Application.Interfaces;
using Microsoft.Extensions.Logging;
using Xcel.Services.Email.Interfaces;
using Xcel.Services.Email.Models;
using Xcel.Services.Email.Templates.TutorApprovalEmail;

namespace Application.UseCases.Commands.TutorApplications.Step2;

public static class TutorApplicationApproveCv
{
    public record Command(Guid TutorApplicationId) : IRequest<Result>;

    public class Handler(
        ITutorApplicationsRepository tutorApplicationsRepository,
        IReviewerAssignmentService reviewerAssignmentService,
        IEmailSender emailSender,
        ILogger<Handler> logger) : IRequestHandler<Command, Result>
    {
        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            logger.LogInformation("[TutorApplicationApproveCv] Attempting to approve CV review for TutorApplicationId: {TutorApplicationId}", request.TutorApplicationId);

            var tutorApplication = await tutorApplicationsRepository.GetByIdAsync(request.TutorApplicationId, cancellationToken);
            if (tutorApplication is null)
            {
                logger.LogError("[TutorApplicationApproveCv] Tutor Application with ID '{TutorApplicationId}' not found.", request.TutorApplicationId);
                return Result.Fail(new Error(ErrorType.NotFound, $"Tutor Application with ID '{request.TutorApplicationId}' not found."));
            }

            var validationResult = tutorApplication.ValidateTutorApplicationForCvReview(logger);
            if (validationResult.IsFailure)
            {
                return validationResult;
            }

            var reviewer = await reviewerAssignmentService.GetAvailableReviewerAsync(cancellationToken);
            if (reviewer is null)
            {
                logger.LogError("[TutorApplicationApproveCv] No reviewer available at the moment for TutorApplicationId: {TutorApplicationId}", request.TutorApplicationId);
                return Result.Fail(new Error(ErrorType.NotFound, "No available reviewer at this time."));
            }

            var interview = new TutorApplicationInterview
            {
                TutorApplicationId = tutorApplication.Id,
                TutorApplication = tutorApplication,
                ReviewerId = reviewer.Id,
                Reviewer = reviewer,
                Status = TutorApplicationInterview.InterviewStatus.AwaitingTutorApplicantProposedDates,
                Platform = TutorApplicationInterview.InterviewPlatform.GoogleMeets
            };

            tutorApplication.Interview = interview;
            tutorApplication.CurrentStep = TutorApplication.OnboardingStep.AwaitingInterviewBooking;

            tutorApplicationsRepository.Update(tutorApplication);
            await tutorApplicationsRepository.SaveChangesAsync(cancellationToken);

            logger.LogInformation("[TutorApplicationApproveCv] Interview created and application updated for TutorApplicationId: {TutorApplicationId}", request.TutorApplicationId);

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
