using Application.Interfaces;

namespace Application.UseCases.Commands.TutorApplicationOnboarding.Moderator.Step2;

public interface IApplicationApproveCvCommand
{
    Task<Result> ExecuteAsync(Guid tutorApplicationId, CancellationToken cancellationToken = default);
}

internal static class ApplicationApproveCvCommandErrors
{
    public static Error TutorApplicationNotFound(Guid id) =>
        new(ErrorType.NotFound, $"Tutor Application with ID '{id}' not found.");

    public static Error ReviewerNotAvailable(Guid id) =>
        new(ErrorType.Unexpected, $"No reviewer available for Tutor Application ID '{id}'.");

    public static Error EmailSendFailed(Guid id) =>
        new(ErrorType.Unexpected, $"Failed to send approval emails for Tutor Application ID: '{id}'");
}

internal sealed class ApplicationApproveCvCommand(
    ITutorApplicationsRepository tutorApplicationsRepository,
    IReviewerAssignmentService reviewerAssignmentService,
    IEmailService emailService,
    ILogger<ApplicationApproveCvCommand> logger
) : IApplicationApproveCvCommand
{
    private const string ServiceName = "[ApplicationApproveCvCommand]";

    public async Task<Result> ExecuteAsync(Guid tutorApplicationId, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("{Service} Approving CV for TutorApplicationId: {Id}", ServiceName, tutorApplicationId);

        var tutorApplication = await tutorApplicationsRepository
            .GetByIdWithDocumentsAndApplicantAsync(tutorApplicationId, cancellationToken);
        if (tutorApplication is null)
        {
            logger.LogError("{Service} Tutor Application {Id} not found", ServiceName, tutorApplicationId);
            return Result.Fail(ApplicationApproveCvCommandErrors.TutorApplicationNotFound(tutorApplicationId));
        }
        
        var validationResult = tutorApplication.ValidateTutorApplicationForCvReview(logger);
        if (validationResult.IsFailure)
        {
            return validationResult;
        }

        var reviewerResult = await reviewerAssignmentService.GetAvailableReviewerAsync(cancellationToken);
        if (reviewerResult.IsFailure)
        {
            logger.LogError("{Service} No reviewer available for TutorApplicationId: {Id}", ServiceName, tutorApplicationId);
            return Result.Fail(reviewerResult.Errors);
        }

        var reviewer = reviewerResult.Value;
        tutorApplication.Interview = new TutorApplicationInterview
        {
            TutorApplicationId = tutorApplication.Id,
            TutorApplication = tutorApplication,
            Reviewer = reviewer,
            Status = TutorApplicationInterview.InterviewStatus.AwaitingApplicantSlotSelection,
            Platform = TutorApplicationInterview.InterviewPlatform.GoogleMeets
        };

        var cvDocument = tutorApplication.Documents.Single(d => d.DocumentType == TutorDocument.TutorDocumentType.Cv);
        cvDocument.Status = TutorDocument.TutorDocumentStatus.Approved;
        tutorApplication.CurrentStep = TutorApplication.OnboardingStep.InterviewBooking;

        logger.LogInformation("{Service} Assigned reviewer and updated interview for TutorApplicationId: {Id}", ServiceName, tutorApplicationId);

        var applicantEmail = new EmailPayload<ApplicantCvApprovalEmail>(
            tutorApplication.Applicant.EmailAddress,
            new(tutorApplication.Applicant.FullName, reviewer.FullName));

        var reviewerEmail = new EmailPayload<ApplicantAssignedToReviewerEmail>(
            reviewer.EmailAddress,
            new(reviewer.FullName, tutorApplication.Applicant.FullName));

        var sendTasks = new[]
        {
            emailService.SendEmailAsync(applicantEmail, cancellationToken),
            emailService.SendEmailAsync(reviewerEmail, cancellationToken)
        };

        await Task.WhenAll(sendTasks);
        if (sendTasks.Any(t => t.Result.IsFailure))
        {
            logger.LogError("{Service} Email send failed for TutorApplicationId: {Id}", ServiceName, tutorApplicationId);
            return Result.Fail(ApplicationApproveCvCommandErrors.EmailSendFailed(tutorApplicationId));
        }

        logger.LogInformation("{Service} Emails sent to Applicant: {ApplicantEmail} and Reviewer: {ReviewerEmail}",
            ServiceName, tutorApplication.Applicant.EmailAddress, reviewer.EmailAddress);

        tutorApplicationsRepository.Update(tutorApplication);
        await tutorApplicationsRepository.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}