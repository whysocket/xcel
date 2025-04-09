namespace Presentation.API.Endpoints.Account.Responses;

public record AuthTokensResponse(
    string AccessToken,
    string RefreshToken);