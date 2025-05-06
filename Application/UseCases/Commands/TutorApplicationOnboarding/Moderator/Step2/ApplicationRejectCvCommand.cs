using Xcel.Services.Auth.Public;

namespace Application.UseCases.Commands.TutorApplicationOnboarding.Moderator.Step2;

public interface IApplicationRejectCvCommand
{
    Task<Result> ExecuteAsync(
        Guid tutorApplicationId,
        string? reason = null,
        CancellationToken cancellationToken = default
    );
}

internal static class ApplicationRejectCvCommandErrors
{
    public static Error TutorApplicationNotFound(Guid id) =>
        new(ErrorType.NotFound, $"Tutor Application with ID '{id}' not found.");

    public static Error EmailSendFailed(string email) =>
        new(ErrorType.Unexpected, $"Failed to send rejection email to: '{email}'");

    public static Error AccountDeletionFailed(Guid applicantId) =>
        new(ErrorType.Unexpected, $"Failed to delete account for ApplicantId: '{applicantId}'");
}

internal sealed class ApplicationRejectCvCommand(
    ITutorApplicationsRepository tutorApplicationsRepository,
    IAuthServiceSdk authServiceSdk,
    IEmailService emailService,
    ILogger<ApplicationRejectCvCommand> logger
) : IApplicationRejectCvCommand
{
    private const string ServiceName = "[ApplicationRejectCvCommand]";

    public async Task<Result> ExecuteAsync(
        Guid tutorApplicationId,
        string? reason = null,
        CancellationToken cancellationToken = default
    )
    {
        logger.LogInformation(
            "{Service} Rejecting CV for TutorApplicationId: {Id}",
            ServiceName,
            tutorApplicationId
        );

        var application = await tutorApplicationsRepository.GetByIdAsync(
            tutorApplicationId,
            cancellationToken
        );
        if (application is null)
        {
            logger.LogError(
                "{Service} Tutor Application {Id} not found",
                ServiceName,
                tutorApplicationId
            );
            return Result.Fail(
                ApplicationRejectCvCommandErrors.TutorApplicationNotFound(tutorApplicationId)
            );
        }

        var validationResult = application.ValidateTutorApplicationForCvReview(logger);
        if (validationResult.IsFailure)
        {
            return validationResult;
        }

        var emailPayload = new EmailPayload<ApplicantCvRejectionEmail>(
            application.Applicant.EmailAddress,
            new(application.Applicant.FullName, reason)
        );

        var emailResult = await emailService.SendEmailAsync(emailPayload, cancellationToken);
        if (emailResult.IsFailure)
        {
            logger.LogError(
                "{Service} Failed to send rejection email to: {Email}",
                ServiceName,
                application.Applicant.EmailAddress
            );
            return Result.Fail(
                ApplicationRejectCvCommandErrors.EmailSendFailed(application.Applicant.EmailAddress)
            );
        }

        logger.LogInformation(
            "{Service} Rejection email sent to: {Email}",
            ServiceName,
            application.Applicant.EmailAddress
        );

        var deleteAccountResult = await authServiceSdk.DeleteAccountAsync(
            application.Applicant.Id,
            cancellationToken
        );
        if (deleteAccountResult.IsFailure)
        {
            logger.LogError(
                "{Service} Failed to delete account for ApplicantId: {ApplicantId}",
                ServiceName,
                application.Applicant.Id
            );
            return Result.Fail(
                ApplicationRejectCvCommandErrors.AccountDeletionFailed(application.Applicant.Id)
            );
        }

        application.IsRejected = true;
        tutorApplicationsRepository.Update(application);
        await tutorApplicationsRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "{Service} Tutor Application rejected for TutorApplicationId: {Id}",
            ServiceName,
            tutorApplicationId
        );
        return Result.Ok();
    }
}
