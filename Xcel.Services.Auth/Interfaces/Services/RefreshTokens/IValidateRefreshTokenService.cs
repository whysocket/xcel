using Domain.Results;
using Xcel.Services.Auth.Models;

namespace Xcel.Services.Auth.Interfaces.Services.RefreshTokens;

internal interface IValidateRefreshTokenService
{
    Task<Result<RefreshTokenEntity>> ValidateRefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default);
}