using Xcel.Services.Auth.Public;

namespace Application.UseCases.Commands.TutorApplicationOnboarding.Reviewer.Step3.AfterInterview;

public interface IRejectInterviewCommand
{
    Task<Result> ExecuteAsync(Guid tutorApplicationId, string? rejectionReason = null, CancellationToken cancellationToken = default);
}

internal static class RejectInterviewCommandErrors
{
    public static Error TutorApplicationNotFound(Guid id) =>
        new(ErrorType.NotFound, $"Tutor application with ID '{id}' not found.");

    public static Error InvalidInterviewState =>
        new(ErrorType.Validation, "Interview must be confirmed before rejection.");

    public static Error EmailSendFailed(string email) =>
        new(ErrorType.Unexpected, $"Failed to send rejection email to '{email}'.");

    public static Error AccountDeletionFailed(Guid applicantId) =>
        new(ErrorType.Unexpected, $"Failed to delete account for applicant ID '{applicantId}'.");
}

internal sealed class RejectInterviewCommand(
    ITutorApplicationsRepository tutorApplicationsRepository,
    IAuthServiceSdk authServiceSdk,
    IEmailService emailService,
    ILogger<RejectInterviewCommand> logger
) : IRejectInterviewCommand
{
    private const string ServiceName = "[RejectInterviewCommand]";

    public async Task<Result> ExecuteAsync(Guid tutorApplicationId, string? rejectionReason = null, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("{Service} Attempting to reject interview for TutorApplicationId: {Id}", ServiceName, tutorApplicationId);

        var application = await tutorApplicationsRepository.GetByIdAsync(tutorApplicationId, cancellationToken);
        if (application == null)
        {
            logger.LogWarning("{Service} Tutor application not found: {Id}", ServiceName, tutorApplicationId);
            return Result.Fail(RejectInterviewCommandErrors.TutorApplicationNotFound(tutorApplicationId));
        }

        if (application.Interview?.Status != TutorApplicationInterview.InterviewStatus.Confirmed)
        {
            logger.LogWarning("{Service} Cannot reject unless interview is confirmed. Current: {Status}", ServiceName, application.Interview?.Status);
            return Result.Fail(RejectInterviewCommandErrors.InvalidInterviewState);
        }

        var rejectionEmail = new EmailPayload<TutorInterviewRejectionEmail>(
            application.Applicant.EmailAddress,
            new(application.Applicant.FullName, rejectionReason)
        );

        var emailResult = await emailService.SendEmailAsync(rejectionEmail, cancellationToken);
        if (emailResult.IsFailure)
        {
            logger.LogError("{Service} Failed to send rejection email: {@Errors}", ServiceName, emailResult.Errors);
            return Result.Fail(RejectInterviewCommandErrors.EmailSendFailed(application.Applicant.EmailAddress));
        }

        var deleteResult = await authServiceSdk.DeleteAccountAsync(application.Applicant.Id, cancellationToken);
        if (deleteResult.IsFailure)
        {
            logger.LogError("{Service} Failed to delete applicant account: {ApplicantId}", ServiceName, application.Applicant.Id);
            return Result.Fail(RejectInterviewCommandErrors.AccountDeletionFailed(application.Applicant.Id));
        }

        application.IsRejected = true;
        tutorApplicationsRepository.Update(application);
        await tutorApplicationsRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation("{Service} Tutor application {Id} rejected and applicant account deleted.", ServiceName, application.Id);

        return Result.Ok();
    }
}
