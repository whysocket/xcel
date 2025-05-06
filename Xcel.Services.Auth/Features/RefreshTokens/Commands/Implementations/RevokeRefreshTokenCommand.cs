using Domain.Results;
using Microsoft.Extensions.Logging;
using Xcel.Services.Auth.Features.RefreshTokens.Commands.Interfaces;
using Xcel.Services.Auth.Interfaces.Repositories;
using Xcel.Services.Auth.Interfaces.Services;

namespace Xcel.Services.Auth.Features.RefreshTokens.Commands.Implementations;

internal static class RevokeRefreshTokenServiceErrors
{
    internal static Error RefreshTokenNotFound() =>
        new(ErrorType.NotFound, "Refresh token not found.");
}

internal sealed class RevokeRefreshTokenCommand(
    TimeProvider timeProvider,
    IRefreshTokensRepository refreshTokensRepository,
    IClientInfoService clientInfoService,
    ILogger<RevokeRefreshTokenCommand> logger
) : IRevokeRefreshTokenCommand
{
    private const string ServiceName = "[RevokeRefreshTokenCommand]";

    public async Task<Result> ExecuteAsync(
        string token,
        CancellationToken cancellationToken = default
    )
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
