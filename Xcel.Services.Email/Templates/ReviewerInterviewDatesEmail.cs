namespace Xcel.Services.Email.Templates;

public record ReviewerInterviewDatesEmail(
    string ApplicantFullName,
    List<DateTime> ProposedDatesUtc,
    string? Observations
);
