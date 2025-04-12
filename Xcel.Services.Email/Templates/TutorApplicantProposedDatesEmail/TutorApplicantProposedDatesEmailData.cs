namespace Xcel.Services.Email.Templates.TutorApplicantProposedDatesEmail;

public record TutorApplicantProposedDatesEmailData(
    string TutorFirstName,
    string TutorLastName,
    List<DateTime> ProposedDates,
    string? Observations
);