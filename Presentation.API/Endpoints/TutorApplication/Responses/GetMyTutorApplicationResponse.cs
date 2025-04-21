using Domain.Entities;

namespace Presentation.API.Endpoints.TutorApplication.Responses;

public record GetMyTutorApplicationResponse(
    Guid Id,
    string Status,
    bool IsRejected,
    List<TutorDocumentResponse> Documents,
    TutorApplicationInterviewResponse? Interview)
{
    public static GetMyTutorApplicationResponse FromDomain(Domain.Entities.TutorApplication application)
    {
        return new GetMyTutorApplicationResponse(
            application.Id,
            application.CurrentStep.ToString(),
            application.IsRejected,
            application.Documents.Select(TutorDocumentResponse.FromDomain).ToList(),
            application.Interview is null ? null : TutorApplicationInterviewResponse.FromDomain(application.Interview)
        );
    }
}

public record TutorDocumentResponse(
    Guid Id,
    string Type,
    string Status,
    string DocumentPath,
    string? ModeratorReason,
    int Version)
{
    public static TutorDocumentResponse FromDomain(TutorDocument document)
    {
        return new TutorDocumentResponse(
            document.Id,
            document.DocumentType.ToString(),
            document.Status.ToString(),
            document.DocumentPath,
            document.ModeratorReason,
            document.Version
        );
    }
}

public record TutorApplicationInterviewResponse(
    Guid Id,
    DateTime? ScheduledAt,
    string Platform,
    string Status,
    List<DateTime> ProposedDates,
    string? Observations,
    ReviewerResponse Reviewer)
{
    public static TutorApplicationInterviewResponse FromDomain(TutorApplicationInterview interview)
    {
        return new TutorApplicationInterviewResponse(
            interview.Id,
            interview.ScheduledAt,
            interview.Platform.ToString(),
            interview.Status.ToString(),
            interview.ProposedDates,
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