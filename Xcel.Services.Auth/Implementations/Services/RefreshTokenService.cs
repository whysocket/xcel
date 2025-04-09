using System.Security.Cryptography;
using Domain.Entities;
using Domain.Results;
using Xcel.Services.Auth.Interfaces.Repositories;
using Xcel.Services.Auth.Interfaces.Services;
using Xcel.Services.Auth.Models;

namespace Xcel.Services.Auth.Implementations.Services;

internal class RefreshTokenService(
    TimeProvider timeProvider,
    IRefreshTokensRepository refreshTokensRepository) : IRefreshTokenService
{
    private const int RefreshTokenExpiryDays = 7; // Example: 7 days expiry

    public async Task<Result<RefreshTokenEntity>> GenerateRefreshTokenAsync(Person person, string ipAddress, CancellationToken cancellationToken = default)
    {
        var refreshToken = new RefreshTokenEntity
        {
            Token = GenerateRefreshTokenString(),
            ExpiresAt = timeProvider.GetUtcNow().AddDays(RefreshTokenExpiryDays).UtcDateTime,
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime,
            CreatedByIp = ipAddress,
            PersonId = person.Id,
        };

        await refreshTokensRepository.AddAsync(refreshToken, cancellationToken);
        await refreshTokensRepository.SaveChangesAsync(cancellationToken);

        return Result.Ok(refreshToken);
    }

    public async Task<Result<RefreshTokenEntity>> ValidateRefreshTokenAsync(string token, string ipAddress, CancellationToken cancellationToken = default)
    {
        var refreshToken = await refreshTokensRepository.GetByTokenAsync(token, cancellationToken);
        if (refreshToken == null || refreshToken.IsRevoked || refreshToken.ExpiresAt < DateTime.UtcNow)
        {
            return Result<RefreshTokenEntity>.Fail(new Error(ErrorType.Unauthorized, "Invalid refresh token."));
        }

        if (refreshToken.ReplacedByToken != null)
        {
            await RevokeRefreshTokenAsync(token, ipAddress, cancellationToken);
            return Result<RefreshTokenEntity>.Fail(new Error(ErrorType.Unauthorized, "Invalid refresh token."));
        }

        refreshToken.ReplacedByToken = GenerateRefreshTokenString();
        refreshToken.RevokedAt = timeProvider.GetUtcNow().UtcDateTime;
        refreshToken.RevokedByIp = ipAddress;

        refreshTokensRepository.Update(refreshToken);
        await refreshTokensRepository.SaveChangesAsync(cancellationToken);

        return Result.Ok(refreshToken);
    }

    public async Task<Result> RevokeRefreshTokenAsync(
        string token, 
        string ipAddress,
        CancellationToken cancellationToken = default)
    {
        var refreshToken = await refreshTokensRepository.GetByTokenAsync(token, cancellationToken);
        if (refreshToken == null)
        {
            return Result.Fail(new Error(ErrorType.NotFound, "Refresh token not found."));
        }

        refreshToken.RevokedAt = timeProvider.GetUtcNow().UtcDateTime;
        refreshToken.RevokedByIp = ipAddress;

        refreshTokensRepository.Update(refreshToken);
        await refreshTokensRepository.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }

    private static string GenerateRefreshTokenString()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }
}