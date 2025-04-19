using Domain.Interfaces.Repositories.Shared;
using Domain.Results;
using Xcel.Services.Auth.Models;

namespace Xcel.Services.Auth.Interfaces.Services;

internal interface IPersonRoleService
{
    Task<Result> AddRoleToPersonAsync(Guid personId, Guid roleId, CancellationToken cancellationToken = default);
    Task<Result<List<PersonRoleEntity>>> GetRolesByPersonIdAsync(Guid personId, CancellationToken cancellationToken = default);

    Task<Result<PageResult<PersonRoleEntity>>> GetAllPersonsRolesByRoleIdAsync(
        Guid roleId,
        PageRequest pageRequest,
        CancellationToken cancellationToken = default);

    Task<Result> RemoveRoleFromPersonAsync(Guid personId, Guid roleId, CancellationToken cancellationToken = default);
}