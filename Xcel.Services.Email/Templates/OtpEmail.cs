using Xcel.Services.Email.Interfaces;

namespace Xcel.Services.Email.Templates;

public record OtpEmail(
    string OtpCode,
    DateTime ExpirationUtc,
    string FullName
) : IEmail
{
    public string Subject => "Your One-Time Password";
}