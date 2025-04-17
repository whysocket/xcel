using Xcel.Services.Email.Interfaces;

namespace Xcel.Services.Email.Templates;

public record TutorInterviewRejectionEmail(
    string FullName,
    string? RejectionReason
) : IEmail
{
    public string Subject => "Thank you for your time — Your application was not successful";
}
