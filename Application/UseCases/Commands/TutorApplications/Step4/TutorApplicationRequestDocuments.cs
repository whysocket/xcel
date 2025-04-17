namespace Application.UseCases.Commands.TutorApplications.Step4;

public static class TutorApplicationRequestDocuments
{
    public record Command(Guid TutorApplicationId) : IRequest<Result>;

    public class Handler(
        ITutorApplicationsRepository tutorApplicationsRepository,
        IEmailService emailService,
        ILogger<Handler> logger
    ) : IRequestHandler<Command, Result>
    {
        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            // 1. Load and validate the tutor application
            var application = await tutorApplicationsRepository.GetByIdAsync(request.TutorApplicationId, cancellationToken);
            if (application is null || application.Interview?.Status != TutorApplicationInterview.InterviewStatus.Confirmed)
            {
                return Result.Fail(new Error(ErrorType.Validation, "Tutor application is not ready for document request."));
            }

            // 2. Add empty document slots for ID and DBS if not already present
            if (!application.Documents.Any(d => d.DocumentType == TutorDocument.TutorDocumentType.Id))
            {
                application.Documents.Add(new TutorDocument
                {
                    DocumentType = TutorDocument.TutorDocumentType.Id,
                    Status = TutorDocument.TutorDocumentStatus.Pending,
                    DocumentPath = "" // Will be updated upon upload
                });
            }

            if (!application.Documents.Any(d => d.DocumentType == TutorDocument.TutorDocumentType.Dbs))
            {
                application.Documents.Add(new TutorDocument
                {
                    DocumentType = TutorDocument.TutorDocumentType.Dbs,
                    Status = TutorDocument.TutorDocumentStatus.Pending,
                    DocumentPath = ""
                });
            }

            // 3. Advance application step
            application.CurrentStep = TutorApplication.OnboardingStep.DocumentsRequested;
            tutorApplicationsRepository.Update(application);
            await tutorApplicationsRepository.SaveChangesAsync(cancellationToken);

            // 4. Notify tutor by email
            var email = new EmailPayload<TutorRequestDocumentsEmail>(
                application.Applicant.EmailAddress,
                new(application.Applicant.FullName)
            );

            var result = await emailService.SendEmailAsync(email, cancellationToken);
            if (result.IsFailure)
            {
                logger.LogError("[RequestDocuments] Failed to send email: {@Errors}", result.Errors);
                return Result.Fail(result.Errors);
            }

            logger.LogInformation("[RequestDocuments] Email sent and status updated for TutorApplicationId: {Id}", request.TutorApplicationId);

            return Result.Ok();
        }
    }
}