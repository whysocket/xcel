using Xcel.Services.Email.Interfaces;

namespace Xcel.Services.Email.Templates;

/// <summary>
/// Sent to both applicant and reviewer when the interview is scheduled.
/// </summary>
public record InterviewScheduledEmail(
    string ApplicantFullName,
    string ReviewerFullName,
    DateTime ScheduledAtUtc
) : IEmail
{
    public string Subject => $"Interview Scheduled: {ApplicantFullName} ↔ {ReviewerFullName}";
}
