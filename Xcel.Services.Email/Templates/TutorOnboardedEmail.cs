using Xcel.Services.Email.Interfaces;

namespace Xcel.Services.Email.Templates;

public record TutorOnboardedEmail(string FullName) : IEmail
{
    public string Subject => "🎉 You're officially onboarded!";
}