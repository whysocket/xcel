namespace Application.UseCases.Queries.TutorApplicationOnboarding.Moderator;

public static class GetPendingCvApplicationById
{
    public static class Errors
    {
        public static Error NotFound = new(ErrorType.NotFound, "Tutor application not found or not in CV review step.");
        
        public static Error InvalidState = new(ErrorType.Conflict, "Tutor application is not in CV review step.");
    }

    public record Query(Guid TutorApplicationId) : IRequest<Result<Response>>;

    public class Handler(
        ITutorApplicationsRepository tutorApplicationsRepository)
        : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var application = await tutorApplicationsRepository.GetByIdWithDocumentsAndApplicantAsync(
                request.TutorApplicationId,
                cancellationToken);

            if (application is null)
            {
                return Result.Fail<Response>(Errors.NotFound);
            }

            if (application.CurrentStep != TutorApplication.OnboardingStep.CvUnderReview)
            {
                return Result.Fail<Response>(Errors.InvalidState);
            }

            var cvDocument = GetLatestCvDocument(application);

            var response = new Response(
                new PersonResponse(application.Applicant.FirstName, application.Applicant.LastName, application.Applicant.EmailAddress),
                cvDocument is null
                    ? null
                    : new CvDocumentResponse(
                        cvDocument.DocumentPath,
                        cvDocument.Status.ToString(),
                        cvDocument.Version,
                        cvDocument.ModeratorReason)
            );

            return Result.Ok(response);
        }
        
        private static TutorDocument? GetLatestCvDocument(TutorApplication tutorApplication) =>
            tutorApplication.Documents
                .Where(d => d.DocumentType == TutorDocument.TutorDocumentType.Cv)
                .OrderByDescending(d => d.Version)
                .FirstOrDefault();
    }

    public record PersonResponse(string FirstName, string LastName, string EmailAddress);

    public record CvDocumentResponse(
        string Path,
        string Status,
        int Version,
        string? ModeratorReason
    );

    public record Response(
        PersonResponse Person,
        CvDocumentResponse? CvDocument
    );
}