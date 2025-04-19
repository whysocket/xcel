using Domain.Results;

namespace Xcel.Services.Auth.Interfaces.Services;

public record AuthTokens(
    string JwtToken,
    string RefreshToken);

internal interface IAuthenticationService
{
    Task<Result<AuthTokens>> LoginWithOtpAsync(string email, string otp, CancellationToken cancellationToken = default);
    Task<Result<AuthTokens>> RefreshTokenAsync(string refreshTokenValue, CancellationToken cancellationToken = default);
}