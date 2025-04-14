namespace Xcel.Services.Email.Templates.InterviewScheduledEmail;

public record InterviewScheduledEmailData(
    string TutorFullName,
    DateTime ScheduledAtUtc
);