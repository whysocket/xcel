namespace Application.UseCases.Commands.TutorApplicationOnboarding.Reviewer.Step3.AfterInterview;

public interface IApproveInterviewCommand
{
    Task<Result> ExecuteAsync(
        Guid tutorApplicationId,
        CancellationToken cancellationToken = default
    );
}

internal static class ApproveInterviewCommandErrors
{
    public static Error InvalidInterviewState(Guid id) =>
        new(
            ErrorType.Validation,
            $"Interview must be confirmed before approval. TutorApplicationId: '{id}'"
        );

    public static Error EmailSendFailed(Guid id) =>
        new(
            ErrorType.Unexpected,
            $"Failed to send document request email for TutorApplicationId: '{id}'"
        );
}

internal sealed class ApproveInterviewCommand(
    ITutorApplicationsRepository tutorApplicationsRepository,
    IEmailService emailService,
    ILogger<ApproveInterviewCommand> logger
) : IApproveInterviewCommand
{
    private const string ServiceName = "[ApproveInterviewCommand]";

    public async Task<Result> ExecuteAsync(
        Guid tutorApplicationId,
        CancellationToken cancellationToken = default
    )
    {
        logger.LogInformation(
            "{Service} Approving interview for TutorApplicationId: {Id}",
            ServiceName,
            tutorApplicationId
        );

        var application = await tutorApplicationsRepository.GetByIdAsync(
            tutorApplicationId,
            cancellationToken
        );
        if (application?.Interview?.Status != TutorApplicationInterview.InterviewStatus.Confirmed)
        {
            logger.LogWarning(
                "{Service} Invalid interview state for TutorApplicationId: {Id}",
                ServiceName,
                tutorApplicationId
            );
            return Result.Fail(
                ApproveInterviewCommandErrors.InvalidInterviewState(tutorApplicationId)
            );
        }

        application.CurrentStep = TutorApplication.OnboardingStep.DocumentsAnalysis;

        var email = new EmailPayload<TutorRequestDocumentsEmail>(
            application.Applicant.EmailAddress,
            new(application.Applicant.FullName)
        );

        var emailResult = await emailService.SendEmailAsync(email, cancellationToken);
        if (emailResult.IsFailure)
        {
            logger.LogError(
                "{Service} Failed to send email for TutorApplicationId: {Id}. Errors: {@Errors}",
                ServiceName,
                tutorApplicationId,
                emailResult.Errors
            );
            return Result.Fail(ApproveInterviewCommandErrors.EmailSendFailed(tutorApplicationId));
        }

        logger.LogInformation(
            "{Service} Interview approved and document submission requested for TutorApplicationId: {Id}",
            ServiceName,
            tutorApplicationId
        );

        await tutorApplicationsRepository.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}
