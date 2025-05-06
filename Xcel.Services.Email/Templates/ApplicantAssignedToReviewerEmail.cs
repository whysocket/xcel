using Xcel.Services.Email.Interfaces;

namespace Xcel.Services.Email.Templates;

public record ApplicantAssignedToReviewerEmail(string ReviewerFullName, string ApplicantFullName)
    : IEmail
{
    public string Subject => $"New applicant assigned for interview: {ApplicantFullName}";
}
