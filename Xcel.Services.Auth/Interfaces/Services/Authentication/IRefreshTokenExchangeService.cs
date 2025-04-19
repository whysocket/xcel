using Domain.Results;
using Xcel.Services.Auth.Models;

namespace Xcel.Services.Auth.Interfaces.Services.Authentication;

internal interface IRefreshTokenExchangeService
{
    Task<Result<AuthTokens>> RefreshTokenAsync(string refreshTokenValue, CancellationToken cancellationToken = default);
}