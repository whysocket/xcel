namespace Xcel.Services.Email.Templates;

public record OtpEmail(
    string OtpCode,
    DateTime ExpirationUtc,
    string FullName
);