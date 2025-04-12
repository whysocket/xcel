namespace Xcel.Services.Email.Templates.ReviewerInterviewDatesEmail;

public record ReviewerInterviewDatesEmailData(
    string ApplicantFullName,
    List<DateTime> ProposedDates,
    string? Observations
);
