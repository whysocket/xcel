using Application.Interfaces;

namespace Application.UseCases.Commands.TutorApplications.Step2;

public static class TutorApplicationApproveCv
{
    public record Command(Guid TutorApplicationId) : IRequest<Result>;

    public class Handler(
        ITutorApplicationsRepository tutorApplicationsRepository,
        IReviewerAssignmentService reviewerAssignmentService,
        IEmailService emailService,
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
            if (reviewer.IsFailure)
            {
                logger.LogError("[TutorApplicationApproveCv] No reviewer available at the moment for TutorApplicationId: {TutorApplicationId}", request.TutorApplicationId);
                return Result.Fail(reviewer.Errors);
            }

            var interview = new TutorApplicationInterview
            {
                TutorApplicationId = tutorApplication.Id,
                TutorApplication = tutorApplication,
                Reviewer = reviewer.Value,
                Status = TutorApplicationInterview.InterviewStatus.AwaitingReviewerProposedDates,
                Platform = TutorApplicationInterview.InterviewPlatform.GoogleMeets
            };

            tutorApplication.Interview = interview;
            tutorApplication.CurrentStep = TutorApplication.OnboardingStep.AwaitingInterviewBooking;

            tutorApplicationsRepository.Update(tutorApplication);
            await tutorApplicationsRepository.SaveChangesAsync(cancellationToken);

            logger.LogInformation("[TutorApplicationApproveCv] Interview created and application updated for TutorApplicationId: {TutorApplicationId}", request.TutorApplicationId);

            var emailPayload = new EmailPayload<TutorApprovalEmail>(
                tutorApplication.Applicant.EmailAddress,
                new TutorApprovalEmail(tutorApplication.Applicant.FullName));

            try
            {
                await emailService.SendEmailAsync(emailPayload, cancellationToken);
                logger.LogInformation("[TutorApplicationApproveCv] Approval email sent to: {Email}", tutorApplication.Applicant.EmailAddress);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[TutorApplicationApproveCv] Failed to send approval email to: {Email}", tutorApplication.Applicant.EmailAddress);
            }

            return Result.Ok();
        }
    }
}
