using Xcel.Services.Email.Interfaces;

namespace Xcel.Services.Email.Templates;

public record ApplicantCvRejectionEmail(string ApplicantFullName, string? RejectionReason) : IEmail
{
    public string Subject => "Update on your Xceltutors application";
}
