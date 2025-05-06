using Xcel.Services.Email.Interfaces;

namespace Xcel.Services.Email.Templates;

public record WelcomeEmail(string FullName) : IEmail
{
    public string Subject => "Welcome to Our Platform!";
}
