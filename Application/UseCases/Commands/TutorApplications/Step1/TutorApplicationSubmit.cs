using Domain.Interfaces.Services;
using Xcel.Services.Auth.Interfaces.Services;

namespace Application.UseCases.Commands.TutorApplications.Step1;

public static class TutorApplicationSubmit
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

            var newPersonResult = await userService.CreateAccountAsync(new Person
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                EmailAddress = request.EmailAddress.ToLowerInvariant()
            }, cancellationToken);

            if (newPersonResult.IsFailure)
            {
                logger.LogError("[TutorApplicationApplicationSubmitted] Failed to create account for email: {Email}, Errors: {@Errors}", request.EmailAddress, newPersonResult.Errors);
                return Result.Fail<Guid>(newPersonResult.Errors);
            }

            logger.LogInformation("[TutorApplicationApplicationSubmitted] Account created for person ID: {ApplicantId}", newPersonResult.Value.Id);

            var cvPathResult = await fileService.UploadAsync(request.CurriculumVitae, cancellationToken);
            if (cvPathResult.IsFailure)
            {
                logger.LogError("[TutorApplicationApplicationSubmitted] Failed to upload Curriculum Vitae.");
                return Result.Fail<Guid>(cvPathResult.Errors);
            }

            var application = new TutorApplication
            {
                ApplicantId = newPersonResult.Value.Id,
                CurrentStep = TutorApplication.OnboardingStep.CvUnderReview,
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

            logger.LogInformation("[TutorApplicationApplicationSubmitted] Tutor Application created with ID: {ApplicationId}", application.Id);

            return Result.Ok(application.Id);
        }
    }
}