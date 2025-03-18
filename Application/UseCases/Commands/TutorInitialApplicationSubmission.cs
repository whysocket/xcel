using Domain.Interfaces.Services;
using Xcel.Services.Auth.Interfaces;

namespace Application.UseCases.Commands;

public static class TutorInitialApplicationSubmission
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

    public class Handler(ITutorsRepository tutorsRepository, 
        IAccountService accountService,
        IFileService fileService) : IRequestHandler<Command, Result<Guid>>
    {
        public async Task<Result<Guid>> Handle(Command request, CancellationToken cancellationToken)
        {
            var newPerson = await accountService.CreateAccountAsync(new Person
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                EmailAddress = request.EmailAddress
            }, cancellationToken);

            if (newPerson.IsFailure)
            {
                return Result<Guid>.Failure(newPerson.ErrorMessage);
            }
            
            // Upload CV
            var cvPath = await fileService.UploadAsync(request.CurriculumVitae, cancellationToken);
            if (string.IsNullOrEmpty(cvPath))
            {
                return Result<Guid>.Failure("Failed to upload Curriculum Vitae.");
            }

            // Create Tutor
            var tutor = new Tutor
            {
                PersonId = newPerson.Value.Id,
                CurrentStep = Tutor.OnboardingStep.DocumentsUploaded,
                Status = Tutor.TutorStatus.Pending,
                TutorDocuments = [
                    new()
                    {
                        DocumentPath = cvPath,
                        DocumentType = TutorDocument.TutorDocumentType.CV,
                        Status = TutorDocument.TutorDocumentStatus.Pending,
                    }
                ]
            };

            await tutorsRepository.AddAsync(tutor, cancellationToken);
            await tutorsRepository.SaveChangesAsync(cancellationToken);

            return Result<Guid>.Success(tutor.Id);
        }
    }
}