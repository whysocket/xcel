namespace Presentation.API.Endpoints.Account;

public record LoginWithOtpRequest(
    string Email,
    string OtpCode);