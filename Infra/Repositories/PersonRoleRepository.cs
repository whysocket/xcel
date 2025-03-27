using Infra.Repositories.Shared;
using Microsoft.EntityFrameworkCore;
using Xcel.Services.Auth.Interfaces.Repositories;
using Xcel.Services.Auth.Models;

namespace Infra.Repositories;

internal class PersonRoleRepository(AppDbContext dbContext) : GenericRepository<PersonRoleEntity>(dbContext), IPersonRoleRepository
{
    public async Task<PersonRoleEntity?> GetPersonRoleAsync(Guid personId, Guid roleId, CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<PersonRoleEntity>()
            .FirstOrDefaultAsync(pr => pr.PersonId == personId && pr.RoleId == roleId, cancellationToken);
    }

    public async Task<List<RoleEntity>> GetRolesForPersonAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<PersonRoleEntity>()
            .Where(pr => pr.PersonId == personId)
            .Select(pr => pr.Role)
            .ToListAsync(cancellationToken);
    }
}