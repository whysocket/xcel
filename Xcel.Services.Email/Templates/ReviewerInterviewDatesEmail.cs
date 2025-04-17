using Xcel.Services.Email.Interfaces;

namespace Xcel.Services.Email.Templates;

public record ReviewerInterviewDatesEmail(
    string ApplicantFullName,
    List<DateTime> ProposedDatesUtc,
    string? Observations
) : IEmail
{
    public string Subject => "A tutor has proposed interview dates";
}
