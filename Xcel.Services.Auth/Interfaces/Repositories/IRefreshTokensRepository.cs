using Domain.Interfaces.Repositories.Shared;
using Xcel.Services.Auth.Models;

namespace Xcel.Services.Auth.Interfaces.Repositories;

public interface IRefreshTokensRepository : IGenericRepository<RefreshTokenEntity>
{
    Task<RefreshTokenEntity?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<List<RefreshTokenEntity>> GetAllByPersonIdAsync(Guid personId, CancellationToken cancellationToken = default);
    Task RevokeRefreshTokensByPersonIdAsync(Guid personId, CancellationToken cancellationToken = default);
}