using Domain.Results;
using Xcel.Services.Auth.Models;

namespace Xcel.Services.Auth.Interfaces.Services.RefreshTokens;

internal interface IGenerateRefreshTokenCommand
{
    Task<Result<RefreshTokenEntity>> GenerateRefreshTokenAsync(
        CancellationToken cancellationToken = default);
}