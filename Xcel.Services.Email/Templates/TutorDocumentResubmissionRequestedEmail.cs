using Xcel.Services.Email.Interfaces;

namespace Xcel.Services.Email.Templates;

public record TutorDocumentResubmissionRequestedEmail(
    string FullName,
    string DocumentType,
    string RejectionReason
) : IEmail
{
    public string Subject => $"Action needed: Please resubmit your {DocumentType} document";
}
