using Xcel.Services.Email.Interfaces;

namespace Xcel.Services.Email.Templates;

public record TutorApplicantProposedDatesEmail(
    string ApplicantFullName,
    List<DateTime> ProposedDatesUtc,
    string? Observations
) : IEmail
{
    public string Subject => "Your reviewer has proposed new interview dates";
}
