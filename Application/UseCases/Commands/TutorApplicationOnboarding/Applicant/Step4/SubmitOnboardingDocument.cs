using Domain.Interfaces.Services;

namespace Application.UseCases.Commands.TutorApplicationOnboarding.Applicant.Step4;

public static class SubmitOnboardingDocument
{
    private static class Errors
    {
        public static class Command
        {
            public static readonly Error InvalidDocumentType =
                new(ErrorType.Validation, "Only ID and DBS documents can be submitted at this stage.");
        }

        public static class Handler
        {
            public static readonly Error ApplicationNotReady =
                new(ErrorType.Validation, "Tutor application is not ready to receive documents.");
        }
    }

    public record Command(Guid TutorApplicationId, TutorDocument.TutorDocumentType Type, DocumentPayload File)
        : IRequest<Result>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator(DocumentPayloadValidator fileValidator)
        {
            RuleFor(x => x.Type)
                .Must(t => t is TutorDocument.TutorDocumentType.Id or TutorDocument.TutorDocumentType.Dbs)
                .WithMessage(Errors.Command.InvalidDocumentType.Message);

            RuleFor(x => x.File)
                .SetValidator(fileValidator);
        }
    }

    public class Handler(
        ITutorApplicationsRepository tutorApplicationsRepository,
        IFileService fileService,
        ITutorDocumentsRepository tutorDocumentsRepository,
        IEmailService emailService,
        ILogger<Handler> logger
    ) : IRequestHandler<Command, Result>
    {
        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            var application =
                await tutorApplicationsRepository.GetByIdAsync(request.TutorApplicationId, cancellationToken);
            if (application?.Interview is null || IsInValidStatus(application))
            {
                return Result.Fail(Errors.Handler.ApplicationNotReady);
            }

            var fileUploadResult = await fileService.UploadAsync(request.File, cancellationToken);
            if (fileUploadResult.IsFailure)
            {
                return Result.Fail(fileUploadResult.Errors);
            }

            var latestDocumentVersion = await tutorDocumentsRepository.GetLatestDocumentVersionByType(
                request.TutorApplicationId,
                request.Type,
                cancellationToken);

            var nextVersion = latestDocumentVersion + 1;

            var newDocument = new TutorDocument
            {
                DocumentType = request.Type,
                DocumentPath = fileUploadResult.Value,
                Status = TutorDocument.TutorDocumentStatus.Pending,
                Version = nextVersion
            };

            application.Documents.Add(newDocument);

            tutorApplicationsRepository.Update(application);
            await tutorApplicationsRepository.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "[SubmitOnboardingDocument] Uploaded {Type} v{Version} for application {Id}",
                request.Type, nextVersion, application.Id);

            var reviewer = application.Interview.Reviewer;
            var email = new EmailPayload<TutorDocumentSubmittedToReviewerEmail>(
                reviewer.EmailAddress,
                new(
                    application.Applicant.FullName,
                    request.Type.ToString(),
                    nextVersion
                )
            );

            var emailResult = await emailService.SendEmailAsync(email, cancellationToken);
            if (emailResult.IsFailure)
            {
                logger.LogError("[SubmitOnboardingDocument] Failed to send reviewer notification: {@Errors}", emailResult.Errors);

                return Result.Fail(emailResult.Errors);
            }

            logger.LogInformation("[SubmitOnboardingDocument] AfterInterview {Email} notified of document submission",
                reviewer.EmailAddress);

            return Result.Ok();
        }
    }

    private static bool IsInValidStatus(TutorApplication application)
    {
        return application.CurrentStep != TutorApplication.OnboardingStep.DocumentsAnalysis
               || application.IsRejected;
    }
}