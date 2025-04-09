namespace Presentation.API.Endpoints.Account.Requests;

public record LoginWithOtpRequest(
    string Email,
    string OtpCode);