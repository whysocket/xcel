using Domain.Results;

namespace Xcel.Services.Auth.Interfaces.Services.RefreshTokens;

internal interface IValidateRefreshTokenService
{
    Task<Result> ValidateRefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default);
}