namespace Xcel.Services.Email.Templates.ReviewerInterviewDatesEmail;

public record ReviewerInterviewDatesEmailData(
    string TutorFullName,
    List<DateTime> ProposedDates,
    string? Observations
);
