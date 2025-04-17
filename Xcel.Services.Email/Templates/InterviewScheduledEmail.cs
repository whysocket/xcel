using Xcel.Services.Email.Interfaces;

namespace Xcel.Services.Email.Templates;

public enum Party
{
    Applicant,
    Reviewer
}

public record InterviewScheduledEmail(
    string RecipientName,
    string InterviewerName,
    DateTime ScheduledAtUtc,
    Party Party) : IEmail
{
    private const string ReviewerConfirmationSubject = "New Interview Scheduled for Review";
    private const string ApplicantConfirmationSubject = "Your Interview Has Been Scheduled";

    public string Subject => Party == Party.Reviewer ? ReviewerConfirmationSubject : ApplicantConfirmationSubject;
}