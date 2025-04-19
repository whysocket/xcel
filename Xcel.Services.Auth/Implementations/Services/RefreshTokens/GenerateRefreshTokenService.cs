using Domain.Entities;
using Domain.Results;
using Microsoft.Extensions.Logging;
using Xcel.Services.Auth.Implementations.Services.RefreshTokens.Common;
using Xcel.Services.Auth.Interfaces.Repositories;
using Xcel.Services.Auth.Interfaces.Services;
using Xcel.Services.Auth.Interfaces.Services.RefreshTokens;
using Xcel.Services.Auth.Models;

namespace Xcel.Services.Auth.Implementations.Services.RefreshTokens;

internal sealed class GenerateRefreshTokenService(
    TimeProvider timeProvider,
    IRefreshTokensRepository refreshTokensRepository,
    IClientInfoService clientInfoService,
    ILogger<GenerateRefreshTokenService> logger) : IGenerateRefreshTokenService
{
    private const string ServiceName = "[GenerateRefreshTokenService]";
    private const int RefreshTokenExpiryDays = 7;

    public async Task<Result<RefreshTokenEntity>> GenerateRefreshTokenAsync(
        Person person,
        CancellationToken cancellationToken = default)
    {
        var refreshToken = new RefreshTokenEntity
        {
            Token = RefreshTokenHelpers.GenerateRefreshTokenString(),
            ExpiresAt = timeProvider.GetUtcNow().AddDays(RefreshTokenExpiryDays).UtcDateTime,
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
            CreatedByIp = clientInfoService.GetIpAddress(),
            PersonId = person.Id,
        };

        await refreshTokensRepository.AddAsync(refreshToken, cancellationToken);
        await refreshTokensRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation($"{ServiceName} - Refresh token generated for personId: {person.Id}.");
        return Result.Ok(refreshToken);
    }
}