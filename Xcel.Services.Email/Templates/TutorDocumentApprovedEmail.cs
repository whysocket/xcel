using Xcel.Services.Email.Interfaces;

namespace Xcel.Services.Email.Templates;

public record TutorDocumentApprovedEmail(string DocumentType) : IEmail
{
    public string Subject => $"Your {DocumentType} document has been approved";
}
