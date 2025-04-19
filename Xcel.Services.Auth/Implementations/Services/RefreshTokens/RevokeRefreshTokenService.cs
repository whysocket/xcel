using Domain.Results;
using Microsoft.Extensions.Logging;
using Xcel.Services.Auth.Interfaces.Repositories;
using Xcel.Services.Auth.Interfaces.Services;
using Xcel.Services.Auth.Interfaces.Services.RefreshTokens;

namespace Xcel.Services.Auth.Implementations.Services.RefreshTokens;

internal static class RevokeRefreshTokenServiceErrors
{
    internal static Error RefreshTokenNotFound() => new(ErrorType.NotFound, "Refresh token not found.");
}

internal sealed class RevokeRefreshTokenService(
    TimeProvider timeProvider,
    IRefreshTokensRepository refreshTokensRepository,
    IClientInfoService clientInfoService,
    ILogger<RevokeRefreshTokenService> logger) : IRevokeRefreshTokenService
{
    private const string ServiceName = "[RevokeRefreshTokenService]";

    public async Task<Result> RevokeRefreshTokenAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        var refreshToken = await refreshTokensRepository.GetByTokenAsync(token, cancellationToken);
        if (refreshToken == null)
        {
            logger.LogWarning($"{ServiceName} - Not Found: Refresh token '{token}' not found.");
            return Result.Fail(RevokeRefreshTokenServiceErrors.RefreshTokenNotFound());
        }

        refreshToken.RevokedAt = timeProvider.GetUtcNow().UtcDateTime;
        refreshToken.RevokedByIp = clientInfoService.IpAddress;

        refreshTokensRepository.Update(refreshToken);
        await refreshTokensRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation($"{ServiceName} - Refresh token '{token}' revoked.");
        return Result.Ok();
    }
}