namespace Xcel.Services.Email.Templates;

public record TutorApplicantProposedDatesEmail(
    string ApplicantFullName,
    List<DateTime> ProposedDatesUtc,
    string? Observations
);