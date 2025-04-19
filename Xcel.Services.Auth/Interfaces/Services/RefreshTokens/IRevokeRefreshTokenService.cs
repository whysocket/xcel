using Domain.Results;

namespace Xcel.Services.Auth.Interfaces.Services.RefreshTokens;

internal interface IRevokeRefreshTokenService
{
    Task<Result> RevokeRefreshTokenAsync(string token, CancellationToken cancellationToken = default);
}