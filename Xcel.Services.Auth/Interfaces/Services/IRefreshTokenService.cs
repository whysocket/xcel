using Domain.Entities;
using Domain.Results;
using Xcel.Services.Auth.Models;

namespace Xcel.Services.Auth.Interfaces.Services;

public interface IRefreshTokenService
{
    Task<Result<RefreshTokenEntity>> GenerateRefreshTokenAsync(Person person, string ipAddress, CancellationToken cancellationToken = default);

    Task<Result<RefreshTokenEntity>> ValidateRefreshTokenAsync(string token, string ipAddress,
        CancellationToken cancellationToken = default);
    Task<Result> RevokeRefreshTokenAsync(string token, string ipAddress, CancellationToken cancellationToken = default);
}