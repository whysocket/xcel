using Domain.Interfaces.Repositories.Shared;
using Xcel.Services.Auth.Models;

namespace Xcel.Services.Auth.Interfaces.Repositories;

public interface IRefreshTokenRepository : IGenericRepository<RefreshTokenEntity>
{
    Task<RefreshTokenEntity?> FindByTokenAsync(string token, CancellationToken cancellationToken = default);
    // You can add more specific methods if needed, for example:
    // Task<IEnumerable<RefreshToken>> GetRefreshTokensByPersonIdAsync(Guid personId, CancellationToken cancellationToken = default);
    // Task RevokeRefreshTokensByPersonIdAsync(Guid personId, CancellationToken cancellationToken = default);
}