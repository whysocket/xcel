using Domain.Entities;
using Domain.Results;
using Xcel.Services.Auth.Interfaces.Services.RefreshTokens;
using Xcel.Services.Auth.Interfaces.Services.RefreshTokens.Facade;
using Xcel.Services.Auth.Models;

namespace Xcel.Services.Auth.Implementations.Services.RefreshTokens.Facade;

internal sealed class RefreshTokenService(
    IGenerateRefreshTokenService generateRefreshTokenService,
    IValidateRefreshTokenService validateRefreshTokenService,
    IRevokeRefreshTokenService revokeRefreshTokenService)
    : IRefreshTokenService
{
    public Task<Result<RefreshTokenEntity>> GenerateRefreshTokenAsync(
        Person person,
        CancellationToken cancellationToken = default)
        => generateRefreshTokenService.GenerateRefreshTokenAsync(person, cancellationToken);

    public Task<Result> ValidateRefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
        => validateRefreshTokenService.ValidateRefreshTokenAsync(refreshToken, cancellationToken);

    public Task<Result> RevokeRefreshTokenAsync(string token, CancellationToken cancellationToken = default)
        => revokeRefreshTokenService.RevokeRefreshTokenAsync(token, cancellationToken);
}