namespace Application.UseCases.Queries.TutorApplicationOnboarding.Moderator;

public static class GetPendingCvApplications
{
    public class Query : IRequest<Result<Response>>;

    public class Handler(
        ITutorApplicationsRepository tutorApplicationsRepository) : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var tutors = await tutorApplicationsRepository.GetAllWithDocumentsAndApplicantByOnboardingStep(
                TutorApplication.OnboardingStep.CvUnderReview,
                cancellationToken);

            var result = tutors.Select(t =>
                new TutorDto(new PersonResponse(t.Applicant.FirstName, t.Applicant.LastName, t.Applicant.EmailAddress),
                    t.Documents.Select(td => new TutorDocumentResponse(
                        td.DocumentPath,
                        td.Status.ToString(),
                        td.DocumentType.ToString()))));

            return Result.Ok(new Response(result));
        }
    }
    
    public record PersonResponse(string FirstName, string LastName, string EmailAddress);

    public record TutorDocumentResponse(string Path, string Status, string Type);
    
    public record TutorDto(
        PersonResponse Person,
        IEnumerable<TutorDocumentResponse> Documents);

    public record Response(
        IEnumerable<TutorDto> TutorsApplications);
}