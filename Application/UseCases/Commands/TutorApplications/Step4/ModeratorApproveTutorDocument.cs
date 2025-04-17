namespace Application.UseCases.Commands.TutorApplications.Step4;

public static class ModeratorApproveTutorDocument
{
    public static class Errors
    {
        public static class Handler
        {
            public static readonly Error NotFound =
                new(ErrorType.NotFound, "Tutor document not found.");
        }
    }

    public record Command(Guid DocumentId) : IRequest<Result>;

    public class Handler(
        ITutorDocumentsRepository tutorDocumentsRepository,
        IEmailService emailService,
        ILogger<Handler> logger
    ) : IRequestHandler<Command, Result>
    {
        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            var document = await tutorDocumentsRepository.GetByIdAsync(request.DocumentId, cancellationToken);
            if (document is null)
            {
                logger.LogWarning("[ApproveDocument] Document not found: {Id}", request.DocumentId);
                return Result.Fail(Errors.Handler.NotFound);
            }

            document.Status = TutorDocument.TutorDocumentStatus.Approved;
            document.ModeratorReason = null;
        
            logger.LogInformation("[ApproveDocument] Document {Id} approved", document.Id);

            var applicant = document.TutorApplication.Applicant;
            var email = new EmailPayload<TutorDocumentApprovedEmail>(
                applicant.EmailAddress,
                new(document.DocumentType.ToString())
            );

            var emailResult = await emailService.SendEmailAsync(email, cancellationToken);
            if (emailResult.IsFailure)
            {
                logger.LogError("[ApproveDocument] Failed to send approval email: {@Errors}", emailResult.Errors);
                return Result.Fail(emailResult.Errors);
            }

            tutorDocumentsRepository.Update(document);
            await tutorDocumentsRepository.SaveChangesAsync(cancellationToken);

            logger.LogInformation("[ApproveDocument] Approval email sent to: {Email}", applicant.EmailAddress);

            return Result.Ok();
        }
    }
}