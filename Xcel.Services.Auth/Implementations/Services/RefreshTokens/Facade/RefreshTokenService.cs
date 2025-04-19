using Domain.Results;
using Xcel.Services.Auth.Interfaces.Services.RefreshTokens;
using Xcel.Services.Auth.Interfaces.Services.RefreshTokens.Facade;
using Xcel.Services.Auth.Models;

namespace Xcel.Services.Auth.Implementations.Services.RefreshTokens.Facade;

internal sealed class RefreshTokenService(
    IGenerateRefreshTokenCommand generateRefreshTokenCommand,
    IValidateRefreshTokenCommand validateRefreshTokenCommand,
    IRevokeRefreshTokenCommand revokeRefreshTokenCommand)
    : IRefreshTokenService
{
    public Task<Result<RefreshTokenEntity>> GenerateRefreshTokenAsync(
        CancellationToken cancellationToken = default)
        => generateRefreshTokenCommand.GenerateRefreshTokenAsync(cancellationToken);

    public Task<Result> ValidateRefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
        => validateRefreshTokenCommand.ValidateRefreshTokenAsync(refreshToken, cancellationToken);

    public Task<Result> RevokeRefreshTokenAsync(string token, CancellationToken cancellationToken = default)
        => revokeRefreshTokenCommand.RevokeRefreshTokenAsync(token, cancellationToken);
}