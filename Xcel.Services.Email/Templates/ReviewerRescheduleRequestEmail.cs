using Xcel.Services.Email.Interfaces;

namespace Xcel.Services.Email.Templates;

public record ReviewerRescheduleRequestEmail(
    string ApplicantFullName,
    string ReviewerFullName,
    string? RescheduleReason) : IEmail
{
    public string Subject => $"AfterInterview {ReviewerFullName} requested a reschedule";
}