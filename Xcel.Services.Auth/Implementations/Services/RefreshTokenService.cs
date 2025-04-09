using System.Security.Cryptography;
using Domain.Entities;
using Domain.Interfaces.Repositories;
using Domain.Results;
using Xcel.Services.Auth.Interfaces.Repositories;
using Xcel.Services.Auth.Interfaces.Services;
using Xcel.Services.Auth.Models;

namespace Xcel.Services.Auth.Implementations.Services;

internal class RefreshTokenService(
    IRefreshTokenRepository refreshTokenRepository,
    IPersonsRepository personRepository) : IRefreshTokenService
{
    private const int RefreshTokenExpiryDays = 7; // Example: 7 days expiry

    public async Task<Result<RefreshTokenEntity>> GenerateRefreshTokenAsync(Person person, string ipAddress, CancellationToken cancellationToken = default)
    {
        var refreshToken = new RefreshTokenEntity
        {
            Token = GenerateRefreshTokenString(),
            Expires = DateTime.UtcNow.AddDays(RefreshTokenExpiryDays),
            Created = DateTime.UtcNow,
            CreatedByIp = ipAddress,
            PersonId = person.Id,
        };

        await refreshTokenRepository.AddAsync(refreshToken, cancellationToken);
        await refreshTokenRepository.SaveChangesAsync(cancellationToken);

        return Result.Ok(refreshToken);
    }

    public async Task<Result<Person>> ValidateRefreshTokenAsync(string token, string ipAddress, CancellationToken cancellationToken = default)
    {
        var refreshToken = await refreshTokenRepository.FindByTokenAsync(token, cancellationToken);
        if (refreshToken == null || refreshToken.IsRevoked || refreshToken.Expires < DateTime.UtcNow)
        {
            return Result<Person>.Fail(new Error(ErrorType.Unauthorized, "Invalid refresh token."));
        }

        if (refreshToken.ReplacedByToken != null)
        {
            await RevokeRefreshTokenAsync(token, ipAddress, cancellationToken);
            return Result<Person>.Fail(new Error(ErrorType.Unauthorized, "Invalid refresh token."));
        }

        var person = await personRepository.GetByIdAsync(refreshToken.PersonId, cancellationToken);
        if (person == null)
        {
            return Result<Person>.Fail(new Error(ErrorType.NotFound, "Person associated with refresh token not found."));
        }

        refreshToken.ReplacedByToken = GenerateRefreshTokenString();
        refreshToken.Revoked = DateTime.UtcNow;
        refreshToken.RevokedByIp = ipAddress;

        refreshTokenRepository.Update(refreshToken);
        await refreshTokenRepository.SaveChangesAsync(cancellationToken);

        return Result.Ok(person);
    }

    public async Task<Result> RevokeRefreshTokenAsync(
        string token, 
        string ipAddress,
        CancellationToken cancellationToken = default)
    {
        var refreshToken = await refreshTokenRepository.FindByTokenAsync(token, cancellationToken);
        if (refreshToken == null)
        {
            return Result.Fail(new Error(ErrorType.NotFound, "Refresh token not found."));
        }

        refreshToken.Revoked = DateTime.UtcNow;
        refreshToken.RevokedByIp = ipAddress;

        refreshTokenRepository.Update(refreshToken);
        await refreshTokenRepository.SaveChangesAsync(cancellationToken);

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