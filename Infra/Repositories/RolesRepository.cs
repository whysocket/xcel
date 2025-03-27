using Infra.Repositories.Shared;
using Microsoft.EntityFrameworkCore;
using Xcel.Services.Auth.Interfaces.Repositories;
using Xcel.Services.Auth.Models;

namespace Infra.Repositories;

internal class RolesRepository(AppDbContext dbContext) : GenericRepository<RoleEntity>(dbContext), IRolesRepository
{
    public async Task<RoleEntity?> GetByNameInsensitiveAsync(string roleName, CancellationToken cancellationToken = default)
    {
        return await DbContext
            .Set<RoleEntity>()
            .FirstOrDefaultAsync(t => t.Name.ToLower() == roleName.ToLower(), cancellationToken);
    }
}