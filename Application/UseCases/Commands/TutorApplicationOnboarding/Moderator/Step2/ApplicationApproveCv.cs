using Application.Interfaces;

namespace Application.UseCases.Commands.TutorApplicationOnboarding.Moderator.Step2;

public static class ApplicationApproveCv
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
                "[ApplicationApproveCv] Attempting to approve CV review for TutorApplicationId: {TutorApplicationId}",
                tutorApplicationId);

            var tutorApplication = await tutorApplicationsRepository.GetByIdWithDocumentsAndApplicantAsync(
                tutorApplicationId,
                cancellationToken).ConfigureAwait(false);

            if (tutorApplication is null)
            {
                logger.LogError(
                    "[ApplicationApproveCv] Tutor Application with ID '{TutorApplicationId}' not found.",
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
                    "[ApplicationApproveCv] No reviewer available at the moment for TutorApplicationId: {TutorApplicationId}",
                    tutorApplicationId);
                return Result.Fail(reviewerResult.Errors);
            }

            var reviewer = reviewerResult.Value;
            var interview = new TutorApplicationInterview
            {
                TutorApplicationId = tutorApplication.Id,
                TutorApplication = tutorApplication,
                Reviewer = reviewer,
                Status = TutorApplicationInterview.InterviewStatus.AwaitingApplicantSlotSelection,
                Platform = TutorApplicationInterview.InterviewPlatform.GoogleMeets
            };

            var cvDocument =
                tutorApplication.Documents.Single(d => d.DocumentType == TutorDocument.TutorDocumentType.Cv);
            cvDocument.Status = TutorDocument.TutorDocumentStatus.Approved;
            tutorApplication.Interview = interview;
            tutorApplication.CurrentStep = TutorApplication.OnboardingStep.InterviewBooking;

            logger.LogInformation(
                "[ApplicationApproveCv] Interview created and application updated for TutorApplicationId: {TutorApplicationId}",
                tutorApplicationId);

            var applicantEmailPayload = new EmailPayload<ApplicantCvApprovalEmail>(
                tutorApplication.Applicant.EmailAddress,
                new(tutorApplication.Applicant.FullName, reviewer.FullName));

            var reviewerEmailPayload = new EmailPayload<ApplicantAssignedToReviewerEmail>(
                reviewer.EmailAddress,
                new(reviewer.FullName, tutorApplication.Applicant.FullName));

            List<Task<Result>> emailsTasks =
            [
                emailService.SendEmailAsync(applicantEmailPayload, cancellationToken),
                emailService.SendEmailAsync(reviewerEmailPayload, cancellationToken)
            ];

            await Task.WhenAll(emailsTasks).ConfigureAwait(false);

            if (emailsTasks.Any(t => t.Result.IsFailure))
            {
                logger.LogError(
                    "[ApplicationApproveCv] Failed to send approval emails for TutorApplicationId: {TutorApplicationId}",
                    tutorApplicationId);

                return Result.Fail(new Error(ErrorType.Unexpected,
                    $"Failed to send approval emails for Tutor Application ID: '{tutorApplicationId}'"));
            }

            logger.LogInformation(
                "[ApplicationApproveCv] Approval emails sent to Applicant: {ApplicantEmail} and AfterInterview: {ReviewerEmail}",
                tutorApplication.Applicant.EmailAddress, reviewer.EmailAddress);


            tutorApplicationsRepository.Update(tutorApplication);
            await tutorApplicationsRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return Result.Ok();
        }
    }
}