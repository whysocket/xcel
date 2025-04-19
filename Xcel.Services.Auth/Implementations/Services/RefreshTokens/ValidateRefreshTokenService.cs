using Domain.Results;
using Microsoft.Extensions.Logging;
using Xcel.Services.Auth.Interfaces.Repositories;
using Xcel.Services.Auth.Interfaces.Services.RefreshTokens;
using Xcel.Services.Auth.Models;

namespace Xcel.Services.Auth.Implementations.Services.RefreshTokens;

internal static class ValidateRefreshTokenServiceErrors
{
    internal static Error InvalidRefreshToken() => new(ErrorType.Unauthorized, "Invalid refresh token.");
}

internal sealed class ValidateRefreshTokenService(
    TimeProvider timeProvider,
    IRefreshTokensRepository refreshTokensRepository,
    ILogger<ValidateRefreshTokenService> logger) : IValidateRefreshTokenService
{
    private const string ServiceName = "[ValidateRefreshTokenService]";

    public async Task<Result<RefreshTokenEntity>> ValidateRefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        var existingRefreshToken = await refreshTokensRepository.GetByTokenAsync(refreshToken, cancellationToken);
        if (existingRefreshToken is null)
        {
            logger.LogWarning($"{ServiceName} - Validation failed: Refresh refreshToken '{refreshToken}' not found.");
            return Result.Fail<RefreshTokenEntity>(ValidateRefreshTokenServiceErrors.InvalidRefreshToken());
        }

        if (existingRefreshToken.ReplacedByToken is not null)
        {
            logger.LogWarning($"{ServiceName} - Validation failed: Refresh refreshToken '{refreshToken}' was replaced.");
            return Result.Fail<RefreshTokenEntity>(ValidateRefreshTokenServiceErrors.InvalidRefreshToken());
        }

        if (existingRefreshToken.IsRevoked)
        {
            logger.LogWarning($"{ServiceName} - Validation failed: Refresh refreshToken '{refreshToken}' is revoked.");
            return Result.Fail<RefreshTokenEntity>(ValidateRefreshTokenServiceErrors.InvalidRefreshToken());
        }

        if (existingRefreshToken.ExpiresAt < timeProvider.GetUtcNow().UtcDateTime)
        {
            logger.LogWarning($"{ServiceName} - Validation failed: Refresh refreshToken '{refreshToken}' has expired.");
            return Result.Fail<RefreshTokenEntity>(ValidateRefreshTokenServiceErrors.InvalidRefreshToken());
        }

        logger.LogInformation($"{ServiceName} - Refresh refreshToken '{refreshToken}' is valid.");

        return Result.Ok(existingRefreshToken);
    }
}