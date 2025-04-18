namespace Application.UseCases.Commands.TutorApplicationOnboarding.Step4.Moderator;

public static class ApproveDocumentAndMaybeOnboard
{
    public static class Errors
    {
        public static class Handler
        {
            public static readonly Error NotFound =
                new(ErrorType.NotFound, "Tutor document not found.");
        }
    }

    private static readonly HashSet<TutorDocument.TutorDocumentType> RequiredDocumentTypes = new()
    {
        TutorDocument.TutorDocumentType.Cv,
        TutorDocument.TutorDocumentType.Id,
        TutorDocument.TutorDocumentType.Dbs
    };

    public record Command(Guid DocumentId) : IRequest<Result>;

    public class Handler(
        ITutorDocumentsRepository tutorDocumentsRepository,
        ITutorApplicationsRepository tutorApplicationsRepository,
        ITutorProfilesRepository tutorProfilesRepository,
        IEmailService emailService,
        ILogger<Handler> logger
    ) : IRequestHandler<Command, Result>
    {
        public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
        {
            var document = await tutorDocumentsRepository.GetByIdAsync(request.DocumentId, cancellationToken);
            if (document is null)
            {
                logger.LogWarning("[ApproveDocumentAndMaybeOnboard] Document not found: {Id}", request.DocumentId);
                return Result.Fail(Errors.Handler.NotFound);
            }

            var application = document.TutorApplication;
            var applicant = application.Applicant;

            document.Status = TutorDocument.TutorDocumentStatus.Approved;
            document.ModeratorReason = null;
            tutorDocumentsRepository.Update(document);

            var isFinalApproval = IsFinalApproval(application);
            var result = isFinalApproval
                ? await FinalizeOnboarding(application, applicant, cancellationToken)
                : await SendStandardApprovalEmail(applicant, document.DocumentType, cancellationToken);

            if (result.IsFailure)
            {
                return result;
            }

            await tutorDocumentsRepository.SaveChangesAsync(cancellationToken);

            logger.LogInformation("[ApproveDocumentAndMaybeOnboard] Document {Id} fully processed", document.Id);
            return Result.Ok();
        }

        private bool IsFinalApproval(TutorApplication application)
        {
            var approvedTypes = application.Documents
                .Where(d => d.Status == TutorDocument.TutorDocumentStatus.Approved)
                .Select(d => d.DocumentType)
                .ToHashSet();

            return RequiredDocumentTypes.All(approvedTypes.Contains) &&
                   application.CurrentStep != TutorApplication.OnboardingStep.Onboarded;
        }

        private async Task<Result> SendStandardApprovalEmail(
            Person applicant,
            TutorDocument.TutorDocumentType type,
            CancellationToken cancellationToken)
        {
            var result = await emailService.SendEmailAsync(
                new EmailPayload<TutorDocumentApprovedEmail>(
                    applicant.EmailAddress,
                    new(type.ToString())
                ),
                cancellationToken
            );

            if (result.IsFailure)
            {
                logger.LogError("[ApproveDocumentAndMaybeOnboard] Failed to send approval email: {@Errors}", result.Errors);
            }

            return result;
        }

        private async Task<Result> FinalizeOnboarding(
            TutorApplication application,
            Person applicant,
            CancellationToken cancellationToken)
        {
            application.CurrentStep = TutorApplication.OnboardingStep.Onboarded;
            tutorApplicationsRepository.Update(application);

            await tutorProfilesRepository.AddAsync(new TutorProfile
            {
                Person = applicant,
                Status = TutorProfile.TutorProfileStatus.PendingConfiguration
            }, cancellationToken);

            var result = await emailService.SendEmailAsync(
                new EmailPayload<TutorOnboardedEmail>(
                    applicant.EmailAddress,
                    new(applicant.FullName)
                ),
                cancellationToken
            );

            if (result.IsFailure)
            {
                logger.LogError("[ApproveDocumentAndMaybeOnboard] Failed to send onboarding email: {@Errors}", result.Errors);
                return Result.Fail(result.Errors);
            }

            logger.LogInformation("[ApproveDocumentAndMaybeOnboard] TutorApplication {Id} marked as Onboarded", application.Id);
            return Result.Ok();
        }
    }
}