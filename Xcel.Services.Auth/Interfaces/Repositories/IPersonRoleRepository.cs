using Domain.Interfaces.Repositories.Shared;
using Xcel.Services.Auth.Models;

namespace Xcel.Services.Auth.Interfaces.Repositories;

public interface IPersonRoleRepository : IGenericRepository<PersonRoleEntity>
{
    Task<PersonRoleEntity?> GetPersonRoleAsync(Guid personId, Guid roleId, CancellationToken cancellationToken = default);

    Task<List<RoleEntity>> GetRolesForPersonAsync(Guid personId, CancellationToken cancellationToken = default);
}