using Domain.Results;
using Microsoft.Extensions.Logging;
using Xcel.Services.Auth.Features.RefreshTokens.Commands.Interfaces;
using Xcel.Services.Auth.Interfaces.Repositories;

namespace Xcel.Services.Auth.Features.RefreshTokens.Commands.Implementations;

internal static class ValidateRefreshTokenServiceErrors
{
    internal static Error InvalidRefreshToken() => new(ErrorType.Unauthorized, "Invalid refresh token.");
}

internal sealed class ValidateRefreshTokenCommand(
    TimeProvider timeProvider,
    IRefreshTokensRepository refreshTokensRepository,
    ILogger<ValidateRefreshTokenCommand> logger) : IValidateRefreshTokenCommand
{
    private const string ServiceName = "[ValidateRefreshTokenCommand]";

    public async Task<Result> ExecuteAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        var existingRefreshToken = await refreshTokensRepository.GetByTokenAsync(refreshToken, cancellationToken);
        if (existingRefreshToken is null)
        {
            logger.LogWarning($"{ServiceName} - Validation failed: Refresh refreshToken '{refreshToken}' not found.");
            return Result.Fail(ValidateRefreshTokenServiceErrors.InvalidRefreshToken());
        }

        if (existingRefreshToken.ReplacedByToken is not null)
        {
            logger.LogWarning($"{ServiceName} - Validation failed: Refresh refreshToken '{refreshToken}' was replaced.");
            return Result.Fail(ValidateRefreshTokenServiceErrors.InvalidRefreshToken());
        }

        if (existingRefreshToken.IsRevoked)
        {
            logger.LogWarning($"{ServiceName} - Validation failed: Refresh refreshToken '{refreshToken}' is revoked.");
            return Result.Fail(ValidateRefreshTokenServiceErrors.InvalidRefreshToken());
        }

        if (existingRefreshToken.ExpiresAt < timeProvider.GetUtcNow().UtcDateTime)
        {
            logger.LogWarning($"{ServiceName} - Validation failed: Refresh refreshToken '{refreshToken}' has expired.");
            return Result.Fail(ValidateRefreshTokenServiceErrors.InvalidRefreshToken());
        }

        logger.LogInformation($"{ServiceName} - Refresh refreshToken '{refreshToken}' is valid.");

        return Result.Ok();
    }
}