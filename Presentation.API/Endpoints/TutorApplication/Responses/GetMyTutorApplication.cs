using System.ComponentModel;
using Domain.Entities;

namespace Presentation.API.Endpoints.TutorApplication.Responses;

public record GetMyTutorApplicationResponse(
    [property: Description("Unique identifier of the tutor application.")]
    Guid Id,
    [property: Description("Current onboarding step of the tutor application.")]
    string Status,
    [property: Description("List of submitted documents for this application.")]
    List<TutorDocumentResponse> Documents,
    [property: Description("Interview details, if an interview has been scheduled.")]
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
    [property: Description("Unique identifier of the document.")]
    Guid Id,
    [property: Description("Type of document (e.g., CV, ID, DBS).")]
    string Type,
    [property: Description("Public path or URL to access the uploaded document.")]
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
    [property: Description("Unique identifier of the interview record.")]
    Guid Id,
    [property: Description("Scheduled date and time of the interview in UTC.")]
    DateTime? ScheduledAt,
    [property: Description("Platform where the interview will take place (e.g., Zoom, Google Meet).")]
    string Platform,
    [property: Description("Current status of the interview (e.g., AwaitingConfirmation, Confirmed).")]
    string Status,
    [property: Description("Optional notes or comments regarding the interview.")]
    string? Observations,
    [property: Description("Details of the reviewer assigned to the interview.")]
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
    [property: Description("Unique identifier of the reviewer.")]
    Guid Id,
    [property: Description("First name of the reviewer.")]
    string FirstName,
    [property: Description("Last name of the reviewer.")]
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