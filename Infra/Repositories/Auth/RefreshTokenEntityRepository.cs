using Infra.Repositories.Shared;
using Microsoft.EntityFrameworkCore;
using Xcel.Services.Auth.Interfaces.Repositories;
using Xcel.Services.Auth.Models;

namespace Infra.Repositories.Auth;

internal class RefreshTokenRepository(
    AppDbContext dbContext) : GenericRepository<RefreshTokenEntity>(dbContext), IRefreshTokenRepository

{
    public async Task<RefreshTokenEntity?> FindByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<RefreshTokenEntity>()
            .Include(rt => rt.Person)
            .FirstOrDefaultAsync(rt => rt.Token == token, cancellationToken);
    }

    public async Task<IEnumerable<RefreshTokenEntity>> GetRefreshTokensByPersonIdAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<RefreshTokenEntity>()
            .Where(rt => rt.PersonId == personId)
            .ToListAsync(cancellationToken);
    }

    public async Task RevokeRefreshTokensByPersonIdAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        var tokens = await DbContext.Set<RefreshTokenEntity>()
            .Where(rt => rt.PersonId == personId && rt.Revoked == null)
            .ToListAsync(cancellationToken);

        tokens.ForEach(token =>
        {
            token.Revoked = DateTime.UtcNow;
        });
    }
}