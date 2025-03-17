namespace Xcel.Services.Email.Templates.OtpEmail;

public record OtpEmailData(
    string OtpCode,
    DateTime Expiration,
    string FullName
);