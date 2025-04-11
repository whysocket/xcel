using Domain.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Xcel.Services.Auth.Interfaces.Services;

namespace Application.UseCases.Commands;

public static class TutorApplicationSubmitted
{
    public record Command(
        string FirstName,
        string LastName,
        string EmailAddress,
        DocumentPayload CurriculumVitae) : IRequest<Result<Guid>>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator(DocumentPayloadValidator documentPayloadValidator)
        {
            RuleFor(c => c.FirstName).NotEmpty().MinimumLength(3).MaximumLength(50);
            RuleFor(c => c.LastName).NotEmpty().MinimumLength(3).MaximumLength(50);
            RuleFor(c => c.EmailAddress).NotEmpty().EmailAddress();
            RuleFor(c => c.CurriculumVitae).SetValidator(documentPayloadValidator);
        }
    }

    public class Handler(
        ILogger<Handler> logger,
        ITutorApplicationsRepository applicationsRepository,
        IUserService userService,
        IFileService fileService) : IRequestHandler<Command, Result<Guid>>
    {
        public async Task<Result<Guid>> Handle(Command request, CancellationToken cancellationToken)
        {
            logger.LogInformation("[TutorApplicationApplicationSubmitted] Tutor Application Application Submitted. Request: {@Request}", request);

            var newPerson = await userService.CreateAccountAsync(new Person
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                EmailAddress = request.EmailAddress
            }, cancellationToken);

            if (newPerson.IsFailure)
            {
                logger.LogError("[TutorApplicationApplicationSubmitted] Failed to create account for email: {Email}, Errors: {@Errors}", request.EmailAddress, newPerson.Errors);
                return Result.Fail<Guid>(new Error(ErrorType.Validation, $"A person with the email address '{request.EmailAddress}' already exists"));
            }

            logger.LogInformation("[TutorApplicationApplicationSubmitted] Account created for person ID: {PersonId}", newPerson.Value.Id);

            var cvPath = await fileService.UploadAsync(request.CurriculumVitae, cancellationToken);
            if (string.IsNullOrEmpty(cvPath))
            {
                logger.LogError("[TutorApplicationApplicationSubmitted] Failed to upload Curriculum Vitae.");
                return Result.Fail<Guid>(new Error(ErrorType.Unexpected, "Failed to upload Curriculum Vitae."));
            }

            var application = new TutorApplication
            {
                PersonId = newPerson.Value.Id,
                CurrentStep = TutorApplication.OnboardingStep.CvUnderReview,
                Documents =
                [
                    new TutorDocument
                    {
                        DocumentPath = cvPath,
                        DocumentType = TutorDocument.TutorDocumentType.Cv,
                        Status = TutorDocument.TutorDocumentStatus.Pending
                    }
                ]
            };

            await applicationsRepository.AddAsync(application, cancellationToken);
            await applicationsRepository.SaveChangesAsync(cancellationToken);

            logger.LogInformation("[TutorApplicationApplicationSubmitted] Tutor Application created with ID: {ApplicationId}", application.Id);

            return Result.Ok(application.Id);
        }
    }
}