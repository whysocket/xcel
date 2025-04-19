using Domain.Results;
using Microsoft.Extensions.Logging;
using Xcel.Services.Auth.Implementations.Services.RefreshTokens.Common;
using Xcel.Services.Auth.Interfaces.Repositories;
using Xcel.Services.Auth.Interfaces.Services;
using Xcel.Services.Auth.Interfaces.Services.RefreshTokens;
using Xcel.Services.Auth.Models;

namespace Xcel.Services.Auth.Implementations.Services.RefreshTokens;

internal sealed class GenerateRefreshTokenCommand(
    TimeProvider timeProvider,
    IRefreshTokensRepository refreshTokensRepository,
    IClientInfoService clientInfoService,
    ILogger<GenerateRefreshTokenCommand> logger) : IGenerateRefreshTokenCommand
{
    private const string ServiceName = "[GenerateRefreshTokenCommand]";
    private const int RefreshTokenExpiryDays = 7;

    public async Task<Result<RefreshTokenEntity>> GenerateRefreshTokenAsync(
        CancellationToken cancellationToken = default)
    {
        var refreshToken = new RefreshTokenEntity
        {
            Token = RefreshTokenHelpers.GenerateRefreshTokenString(),
            ExpiresAt = timeProvider.GetUtcNow().AddDays(RefreshTokenExpiryDays).UtcDateTime,
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
            CreatedByIp = clientInfoService.IpAddress,
            PersonId = clientInfoService.PersonId,
        };

        await refreshTokensRepository.AddAsync(refreshToken, cancellationToken);
        await refreshTokensRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation($"{ServiceName} - Refresh token generated for personId: {clientInfoService.PersonId}.");
        return Result.Ok(refreshToken);
    }
}