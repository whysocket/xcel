namespace Application.UseCases.Queries.TutorApplicationOnboarding.Moderator;

public static class GetApplicationById
{
    public static class Errors
    {
        public static Error NotFound => new(ErrorType.NotFound, "Tutor application not found or does not match onboarding step.");
    }

    public record Query(Guid TutorApplicationId)
        : IRequest<Result<Response>>;

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

            var response = new Response(
                application.Id,
                new PersonResponse(
                    application.Applicant.FirstName,
                    application.Applicant.LastName,
                    application.Applicant.EmailAddress),
                application.Documents.Select(d => new TutorDocumentResponse(
                    d.Id,
                    d.DocumentPath,
                    d.Status.ToString(),
                    d.DocumentType.ToString(),
                    d.Version,
                    d.ModeratorReason)).ToList()
            );

            return Result.Ok(response);
        }
    }

    public record PersonResponse(
        string FirstName,
        string LastName,
        string EmailAddress);

    public record TutorDocumentResponse(
        Guid DocumentId,
        string Path,
        string Status,
        string Type,
        int Version,
        string? ModeratorReason);

    public record Response(
        Guid TutorApplicationId,
        PersonResponse Person,
        List<TutorDocumentResponse> Documents);
}