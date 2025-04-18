namespace Application.UseCases.Queries.TutorApplicationOnboarding.Common;

public static class GetInterviewDetailsByParty
{
    public enum Party
    {
        Applicant,
        Reviewer
    }

    public static class Errors
    {
        public static readonly Error NotFound =
            new(ErrorType.NotFound, "Interview not found.");

        public static readonly Error Unauthorized =
            new(ErrorType.Forbidden, "You are not authorized to access this interview.");
    }

    public record Query(Guid TutorApplicationId, Guid UserId, Party PartyRole) : IRequest<Result<Response>>;

    public class Handler(
        ITutorApplicationsRepository repository)
        : IRequestHandler<Query, Result<Response>>
    {
        public async Task<Result<Response>> Handle(Query request, CancellationToken cancellationToken)
        {
            var application = await repository.GetByIdWithInterviewAndPeopleAsync(request.TutorApplicationId, cancellationToken);

            if (application?.Interview is null)
            {
                return Result.Fail<Response>(Errors.NotFound);
            }

            var isValid =
                request.PartyRole == Party.Applicant && application.Applicant.Id == request.UserId ||
                request.PartyRole == Party.Reviewer && application.Interview.Reviewer.Id == request.UserId;

            if (!isValid)
                return Result.Fail<Response>(Errors.Unauthorized);

            var interview = application.Interview;

            var response = new Response(
                application.Id,
                interview.Status.ToString(),
                interview.Platform.ToString(),
                interview.ProposedDates,
                interview.Observations,
                new PersonDto(interview.Reviewer.FullName, interview.Reviewer.EmailAddress),
                new PersonDto(application.Applicant.FullName, application.Applicant.EmailAddress)
            );

            return Result.Ok(response);
        }
    }

    public record PersonDto(string FullName, string Email);

    public record Response(
        Guid ApplicationId,
        string Status,
        string Platform,
        List<DateTime> ProposedDates,
        string? Observations,
        PersonDto Reviewer,
        PersonDto Applicant);
}