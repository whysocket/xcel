using Domain.Interfaces.Repositories.Shared;
using Infra.Repositories.Extensions;
using Infra.Repositories.Shared;
using Microsoft.EntityFrameworkCore;
using Xcel.Services.Auth.Interfaces.Repositories;
using Xcel.Services.Auth.Models;

namespace Infra.Repositories.Auth;

internal class PersonRoleRepository(AppDbContext dbContext) : GenericRepository<PersonRoleEntity>(dbContext), IPersonRoleRepository
{
    public async Task<PersonRoleEntity?> GetPersonRoleAsync(Guid personId, Guid roleId, CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<PersonRoleEntity>()
            .FirstOrDefaultAsync(pr => pr.PersonId == personId && pr.RoleId == roleId, cancellationToken);
    }

    public async Task<PageResult<PersonRoleEntity>> GetRolesForPersonAsync(
        Guid personId, 
        PageRequest request,
        CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<PersonRoleEntity>()
            .Include(pr => pr.Role)
            .Where(pr => pr.PersonId == personId)
            .WithPaginationAsync(request, cancellationToken);
    }

    public async Task<PageResult<PersonRoleEntity>> GetAllPersonsRolesByRoleIdAsync(
        Guid roleId,
        PageRequest pageRequest,
        CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<PersonRoleEntity>()
            .Include(pr => pr.Person)
            .Where(pr => pr.RoleId == roleId)
            .WithPaginationAsync(pageRequest, cancellationToken);
    }
}