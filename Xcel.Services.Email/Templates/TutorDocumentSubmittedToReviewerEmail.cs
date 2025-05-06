using Xcel.Services.Email.Interfaces;

namespace Xcel.Services.Email.Templates;

public record TutorDocumentSubmittedToReviewerEmail(
    string TutorName,
    string DocumentType,
    int NewVersion
) : IEmail
{
    public string Subject => $"Tutor {TutorName} submitted a {DocumentType} document";
}
