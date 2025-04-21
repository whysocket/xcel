using Domain.Results;
using Microsoft.Extensions.Logging;
using Xcel.Services.Auth.Features.RefreshTokens.Commands.Interfaces;
using Xcel.Services.Auth.Features.RefreshTokens.Common;
using Xcel.Services.Auth.Interfaces.Repositories;
using Xcel.Services.Auth.Interfaces.Services;
using Xcel.Services.Auth.Models;

namespace Xcel.Services.Auth.Features.RefreshTokens.Commands.Implementations;

internal sealed class GenerateRefreshTokenCommand(
    TimeProvider timeProvider,
    IRefreshTokensRepository refreshTokensRepository,
    IClientInfoService clientInfoService,
    ILogger<GenerateRefreshTokenCommand> logger) : IGenerateRefreshTokenCommand
{
    private const string ServiceName = "[GenerateRefreshTokenCommand]";
    private const int RefreshTokenExpiryDays = 7;

    public async Task<Result<RefreshTokenEntity>> ExecuteAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var refreshToken = new RefreshTokenEntity
        {
            Token = RefreshTokenHelpers.GenerateRefreshTokenString(),
            ExpiresAt = timeProvider.GetUtcNow().AddDays(RefreshTokenExpiryDays).UtcDateTime,
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
            CreatedByIp = clientInfoService.IpAddress,
            PersonId = userId,
        };

        await refreshTokensRepository.AddAsync(refreshToken, cancellationToken);
        await refreshTokensRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation($"{ServiceName} - Refresh token generated for userId: {userId}.");
        return Result.Ok(refreshToken);
    }
}