using Domain.Interfaces.Services;
using Xcel.Services.Auth.Public;

namespace Application.UseCases.Commands.TutorApplicationOnboarding.Applicant.Step1;

internal static class TutorApplicationSubmitValidator
{
    public static Result Validate(TutorApplicationSubmitInput input)
    {
        var errors = new List<Error>();

        if (string.IsNullOrWhiteSpace(input.FirstName) || input.FirstName.Length < 3 || input.FirstName.Length > 50)
        {
            errors.Add(new Error(ErrorType.Validation, "First name must be between 3 and 50 characters."));
        }

        if (string.IsNullOrWhiteSpace(input.LastName) || input.LastName.Length < 3 || input.LastName.Length > 50)
        {
            errors.Add(new Error(ErrorType.Validation, "Last name must be between 3 and 50 characters."));
        }

        if (string.IsNullOrWhiteSpace(input.EmailAddress) || !IsValidEmail(input.EmailAddress))
        {
            errors.Add(new Error(ErrorType.Validation, "A valid email address is required."));
        }

        if (string.IsNullOrWhiteSpace(input.CurriculumVitae.FileName))
        {
            errors.Add(new Error(ErrorType.Validation, "CV filename is required."));
        }

        if (input.CurriculumVitae.Content.Length == 0)
        {
            errors.Add(new Error(ErrorType.Validation, "CV content must not be empty."));
        }

        return errors.Count == 0 ? Result.Ok() : Result.Fail(errors);
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            _ = new System.Net.Mail.MailAddress(email);
            return true;
        }
        catch
        {
            return false;
        }
    }
}


public interface ITutorApplicationSubmitCommand
{
    Task<Result<Guid>> ExecuteAsync(TutorApplicationSubmitInput input, CancellationToken cancellationToken = default);
}

public sealed class TutorApplicationSubmitInput(
    string firstName,
    string lastName,
    string emailAddress,
    DocumentPayload curriculumVitae)
{
    public string FirstName { get; } = firstName;
    public string LastName { get; } = lastName;
    public string EmailAddress { get; } = emailAddress;
    public DocumentPayload CurriculumVitae { get; } = curriculumVitae;
}

internal sealed class TutorApplicationSubmitCommand(
    ILogger<TutorApplicationSubmitCommand> logger,
    ITutorApplicationsRepository applicationsRepository,
    IAuthServiceSdk authServiceSdk,
    IFileService fileService
) : ITutorApplicationSubmitCommand
{
    private const string ServiceName = "[TutorApplicationSubmitCommand]";

    public async Task<Result<Guid>> ExecuteAsync(TutorApplicationSubmitInput input, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("{Service} Submitting tutor application: {@Input}", ServiceName, input);

        var validation = TutorApplicationSubmitValidator.Validate(input);
        if (validation.IsFailure)
        {
            return Result.Fail<Guid>(validation.Errors);
        }

        var newPersonResult = await authServiceSdk.CreateAccountAsync(new Person
        {
            FirstName = input.FirstName,
            LastName = input.LastName,
            EmailAddress = input.EmailAddress.ToLowerInvariant()
        }, cancellationToken);

        if (newPersonResult.IsFailure)
        {
            logger.LogError("{Service} Failed to create account: {Email}", ServiceName, input.EmailAddress);
            return Result.Fail<Guid>(newPersonResult.Errors);
        }

        var cvPathResult = await fileService.UploadAsync(input.CurriculumVitae, cancellationToken);
        if (cvPathResult.IsFailure)
        {
            logger.LogError("{Service} Failed to upload CV", ServiceName);
            return Result.Fail<Guid>(cvPathResult.Errors);
        }

        var application = new TutorApplication
        {
            ApplicantId = newPersonResult.Value.Id,
            CurrentStep = TutorApplication.OnboardingStep.CvAnalysis,
            Documents =
            [
                new TutorDocument
                {
                    DocumentPath = cvPathResult.Value,
                    DocumentType = TutorDocument.TutorDocumentType.Cv,
                    Status = TutorDocument.TutorDocumentStatus.Pending
                }
            ]
        };

        await applicationsRepository.AddAsync(application, cancellationToken);
        await applicationsRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation("{Service} Tutor application created with ID: {Id}", ServiceName, application.Id);

        return Result.Ok(application.Id);
    }
}