using Domain.Entities;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Services;
using Domain.Payloads;
using Domain.Results;
using FluentValidation;
using MediatR;

namespace Domain.UseCases;

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

    public class Handler(ITutorsRepository tutorsRepository, IAccountService accountService, IFileService fileService) : IRequestHandler<Command, Result<Guid>>
    {
        public async Task<Result<Guid>> Handle(Command request, CancellationToken cancellationToken)
        {
            // Check if account exists
            var accountExists = await accountService.CheckAccountExistanceByEmailAsync(request.EmailAddress, cancellationToken);
            if (accountExists)
            {
                return Result<Guid>.Failure($"Account with email '{request.EmailAddress}' already exists.");
            }

            // Create Person
            var person = new Person
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                EmailAddress = request.EmailAddress
            };

            var createdPerson = await accountService.CreateAccountAsync(person, cancellationToken);

            // Upload CV
            var cvPath = await fileService.UploadAsync(request.CurriculumVitae, cancellationToken);
            if (string.IsNullOrEmpty(cvPath))
            {
                return Result<Guid>.Failure("Failed to upload Curriculum Vitae.");
            }

            // Create Tutor
            var tutor = new Tutor
            {
                PersonId = createdPerson.Id,
                CurrentStep = Tutor.OnboardingStep.DocumentsUploaded,
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