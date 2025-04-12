namespace Xcel.Services.Email.Templates.TutorApplicantProposedDatesEmail;

public record TutorApplicantProposedDatesEmailData(
    string ApplicantFullName,
    List<DateTime> ProposedDates,
    string? Observations
);