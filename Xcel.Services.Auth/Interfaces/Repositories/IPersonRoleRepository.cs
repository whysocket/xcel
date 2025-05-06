using Domain.Interfaces.Repositories.Shared;
using Xcel.Services.Auth.Models;

namespace Xcel.Services.Auth.Interfaces.Repositories;

internal interface IPersonRoleRepository : IGenericRepository<PersonRoleEntity>
{
    Task<PersonRoleEntity?> GetPersonRoleAsync(
        Guid personId,
        Guid roleId,
        CancellationToken cancellationToken = default
    );

    Task<PageResult<PersonRoleEntity>> GetRolesForPersonAsync(
        Guid personId,
        PageRequest request,
        CancellationToken cancellationToken = default
    );

    Task<PageResult<PersonRoleEntity>> GetAllPersonsRolesByRoleIdAsync(
        Guid roleId,
        PageRequest pageRequest,
        CancellationToken cancellationToken = default
    );
}
