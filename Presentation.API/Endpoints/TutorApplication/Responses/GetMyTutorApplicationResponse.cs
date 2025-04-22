using Domain.Entities;

namespace Presentation.API.Endpoints.TutorApplication.Responses;

public record GetMyTutorApplicationResponse(
    Guid Id,
    string Status,
    List<TutorDocumentResponse> Documents,
    TutorApplicationInterviewResponse? Interview)
{
    public static GetMyTutorApplicationResponse FromDomain(Domain.Entities.TutorApplication application)
    {
        return new GetMyTutorApplicationResponse(
            application.Id,
            application.CurrentStep.ToString(),
            application.Documents.Select(TutorDocumentResponse.FromDomain).ToList(),
            application.Interview is null ? null : TutorApplicationInterviewResponse.FromDomain(application.Interview)
        );
    }
}

public record TutorDocumentResponse(
    Guid Id,
    string Type,
    string DocumentPath)
{
    public static TutorDocumentResponse FromDomain(TutorDocument document)
    {
        return new TutorDocumentResponse(
            document.Id,
            document.DocumentType.ToString(),
            document.DocumentPath
        );
    }
}

public record TutorApplicationInterviewResponse(
    Guid Id,
    DateTime? ScheduledAt,
    string Platform,
    string Status,
    string? Observations,
    ReviewerResponse Reviewer)
{
    public static TutorApplicationInterviewResponse FromDomain(TutorApplicationInterview interview)
    {
        return new TutorApplicationInterviewResponse(
            interview.Id,
            interview.ScheduledAtUtc,
            interview.Platform.ToString(),
            interview.Status.ToString(),
            interview.Observations,
            ReviewerResponse.FromDomain(interview.Reviewer)
        );
    }
}

public record ReviewerResponse(
    Guid Id,
    string FirstName,
    string LastName)
{
    public static ReviewerResponse FromDomain(Person reviewer)
    {
        return new ReviewerResponse(
            reviewer.Id,
            reviewer.FirstName,
            reviewer.LastName
        );
    }
}