namespace Xcel.Services.Email.Templates;

public record InterviewScheduledEmail(
    string ApplicantFullName,
    string ReviewerFullName,
    DateTime ScheduledAtUtc);