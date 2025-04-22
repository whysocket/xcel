using Application.Interfaces;

namespace Application.UseCases.Commands.TutorApplicationOnboarding.Step2;

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
            var tutorApplicationId = request.TutorApplicationId;
            logger.LogInformation(
                "[TutorApplicationApproveCv] Attempting to approve CV review for TutorApplicationId: {TutorApplicationId}",
                tutorApplicationId);

            var tutorApplication = await tutorApplicationsRepository.GetByIdWithDocumentsAndApplicantAsync(
                tutorApplicationId,
                cancellationToken).ConfigureAwait(false);

            if (tutorApplication is null)
            {
                logger.LogError(
                    "[TutorApplicationApproveCv] Tutor Application with ID '{TutorApplicationId}' not found.",
                    tutorApplicationId);
                return Result.Fail(new Error(ErrorType.NotFound,
                    $"Tutor Application with ID '{tutorApplicationId}' not found."));
            }

            var validationResult = tutorApplication.ValidateTutorApplicationForCvReview(logger);
            if (validationResult.IsFailure)
            {
                return validationResult;
            }

            var reviewerResult = await reviewerAssignmentService.GetAvailableReviewerAsync(cancellationToken)
                .ConfigureAwait(false);

            if (reviewerResult.IsFailure)
            {
                logger.LogError(
                    "[TutorApplicationApproveCv] No reviewer available at the moment for TutorApplicationId: {TutorApplicationId}",
                    tutorApplicationId);
                return Result.Fail(reviewerResult.Errors);
            }

            var reviewer = reviewerResult.Value;
            var interview = new TutorApplicationInterview
            {
                TutorApplicationId = tutorApplication.Id,
                TutorApplication = tutorApplication,
                Reviewer = reviewer,
                Status = TutorApplicationInterview.InterviewStatus.AwaitingReviewerProposedDates,
                Platform = TutorApplicationInterview.InterviewPlatform.GoogleMeets
            };

            tutorApplication.Interview = interview;
            tutorApplication.CurrentStep = TutorApplication.OnboardingStep.AwaitingInterviewBooking;

            logger.LogInformation(
                "[TutorApplicationApproveCv] Interview created and application updated for TutorApplicationId: {TutorApplicationId}",
                tutorApplicationId);

            var applicantEmailPayload = new EmailPayload<ApplicantCvApprovalEmail>(
                tutorApplication.Applicant.EmailAddress,
                new(tutorApplication.Applicant.FullName, reviewer.FullName));

            var reviewerEmailPayload = new EmailPayload<ApplicantAssignedToReviewerEmail>(
                reviewer.EmailAddress,
                new(reviewer.FullName, tutorApplication.Applicant.FullName));

            try
            {
                await Task.WhenAll(
                        emailService.SendEmailAsync(applicantEmailPayload, cancellationToken),
                        emailService.SendEmailAsync(reviewerEmailPayload, cancellationToken))
                    .ConfigureAwait(false);

                logger.LogInformation(
                    "[TutorApplicationApproveCv] Approval emails sent to Applicant: {ApplicantEmail} and Reviewer: {ReviewerEmail}",
                    tutorApplication.Applicant.EmailAddress, reviewer.EmailAddress);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "[TutorApplicationApproveCv] Failed to send approval emails for TutorApplicationId: {TutorApplicationId}",
                    tutorApplicationId);
                return Result.Fail(new Error(ErrorType.Unexpected,
                    $"Failed to send approval emails for Tutor Application ID: '{tutorApplicationId}'"));
            }
            
            tutorApplicationsRepository.Update(tutorApplication);
            await tutorApplicationsRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return Result.Ok();
        }
    }
}