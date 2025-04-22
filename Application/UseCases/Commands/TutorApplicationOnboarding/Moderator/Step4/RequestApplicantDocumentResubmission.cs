namespace Application.UseCases.Commands.TutorApplicationOnboarding.Moderator.Step4;

public static class RequestApplicantDocumentResubmission
{
    public static class Errors
    {
        public static class Command
        {
            public static string RejectReasonIsRequired = "Rejection reason is required.";
        }
        
        public static class Handler
        {
            public static readonly Error NotFound =
                new(ErrorType.NotFound, "Tutor document not found.");
        }
    }

    public record Command(Guid DocumentId, string RejectionReason) : IRequest<Result>;

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.RejectionReason)
                .NotEmpty().WithMessage(Errors.Command.RejectReasonIsRequired)
                .MaximumLength(1000);
        }
    }

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
                logger.LogWarning("[RequestApplicantDocumentResubmission] Document not found: {Id}", request.DocumentId);
                return Result.Fail(Errors.Handler.NotFound);
            }

            document.Status = TutorDocument.TutorDocumentStatus.ResubmissionNeeded;
            document.ModeratorReason = request.RejectionReason;
            
            logger.LogInformation("[RequestApplicantDocumentResubmission] Resubmission requested for document {Id}", document.Id);

            // Send email to tutor
            var applicant = document.TutorApplication.Applicant;
            var email = new EmailPayload<TutorDocumentResubmissionRequestedEmail>(
                applicant.EmailAddress,
                new(applicant.FullName, document.DocumentType.ToString(), document.ModeratorReason)
            );

            var emailResult = await emailService.SendEmailAsync(email, cancellationToken);
            if (emailResult.IsFailure)
            {
                logger.LogError("[RequestApplicantDocumentResubmission] Failed to send resubmission email: {@Errors}", emailResult.Errors);
                return Result.Fail(emailResult.Errors);
            }

            logger.LogInformation("[RequestApplicantDocumentResubmission] Resubmission email sent to: {Email}", applicant.EmailAddress);

            tutorDocumentsRepository.Update(document);
            await tutorDocumentsRepository.SaveChangesAsync(cancellationToken);

            return Result.Ok();
        }
    }
}