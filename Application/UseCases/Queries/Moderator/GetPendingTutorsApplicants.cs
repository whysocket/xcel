namespace Application.UseCases.Queries.Moderator;

public static class GetPendingTutorsApplicants
{
    public class Query : IRequest<Result<Response>>;

    public record PersonDto(string FirstName, string LastName, string EmailAddress);

    public record TutorDocumentDto(string Path, string Status, string Type);

    public record TutorDto(
        PersonDto Person,
        IEnumerable<TutorDocumentDto> Documents);

    public record Response(
        IEnumerable<TutorDto> TutorsApplications);

    public class Handler(
        ITutorApplicationsRepository tutorApplicationsRepository) : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var tutors = await tutorApplicationsRepository.GetAllPendingTutorsWithDocuments(cancellationToken);

            var result = tutors.Select(t =>
                new TutorDto(new PersonDto(t.Applicant.FirstName, t.Applicant.LastName, t.Applicant.EmailAddress),
                    t.Documents.Select(td => new TutorDocumentDto(
                        td.DocumentPath,
                        td.Status.ToString(),
                        td.DocumentType.ToString()))));

            return Result.Ok(new Response(result));
        }
    }
}