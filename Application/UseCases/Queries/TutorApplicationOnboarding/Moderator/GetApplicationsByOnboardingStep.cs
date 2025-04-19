namespace Application.UseCases.Queries.TutorApplicationOnboarding.Moderator;

public static class GetApplicationsByOnboardingStep
{
    public record Query(TutorApplication.OnboardingStep OnboardingStep) : IRequest<Result<Response>>;

    public class Handler(
        ITutorApplicationsRepository tutorApplicationsRepository,
        ILogger<Handler> logger)
        : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            logger.LogInformation("[GetApplicationsByOnboardingStep] Fetching tutor applications at step: {Step}",
                request.OnboardingStep);

            var tutors = await tutorApplicationsRepository.GetAllWithDocumentsAndApplicantByOnboardingStep(
                request.OnboardingStep,
                cancellationToken);

            logger.LogInformation("[GetApplicationsByOnboardingStep] Found {Count} tutor application(s).", tutors.Count);

            var result = tutors.Select(t =>
                new TutorDto(
                    t.Id,
                    new PersonResponse(
                        t.Applicant.FirstName,
                        t.Applicant.LastName,
                        t.Applicant.EmailAddress),
                    t.Documents.Select(td => new TutorDocumentResponse(
                        td.Id,
                        td.DocumentPath,
                        td.Status.ToString(),
                        td.DocumentType.ToString(),
                        td.Version)).ToList()
                )).ToList();

            return Result.Ok(new Response(result));
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
        int Version);

    public record TutorDto(
        Guid TutorApplicationId,
        PersonResponse Person,
        List<TutorDocumentResponse> Documents);

    public record Response(List<TutorDto> TutorsApplications);
}