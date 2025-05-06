using Xcel.Services.Email.Interfaces;

namespace Xcel.Services.Email.Templates;

public record ApplicantCvApprovalEmail(string ApplicantFullName, string ReviewerFullName) : IEmail
{
    public string Subject => "Your CV has been approved — next step: schedule your interview";
}
