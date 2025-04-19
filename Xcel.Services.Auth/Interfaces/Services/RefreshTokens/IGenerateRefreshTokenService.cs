using Domain.Entities;
using Domain.Results;
using Xcel.Services.Auth.Models;

namespace Xcel.Services.Auth.Interfaces.Services.RefreshTokens;

internal interface IGenerateRefreshTokenService
{
    Task<Result<RefreshTokenEntity>> GenerateRefreshTokenAsync(
        Person person,
        CancellationToken cancellationToken = default);
}