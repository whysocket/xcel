using Domain.Interfaces.Repositories.Shared;
using Domain.Results;
using Xcel.Services.Auth.Interfaces.Services.Roles;
using Xcel.Services.Auth.Models;

namespace Xcel.Services.Auth.Implementations.Services.Roles;

internal sealed class RoleService(
    ICreateRoleService createRoleService,
    IGetAllRolesService getAllRolesService,
    IGetRoleByNameService getRoleByNameService,
    IUpdateRoleService updateRoleService,
    IDeleteRoleByNameService deleteRoleByNameService)
    : IRoleService
{
    public Task<Result<RoleEntity>> CreateRoleAsync(string roleName, CancellationToken cancellationToken = default)
    {
        return createRoleService.CreateRoleAsync(roleName, cancellationToken);
    }

    public Task<Result<PageResult<RoleEntity>>> GetAllRolesAsync(PageRequest pageRequest, CancellationToken cancellationToken = default)
    {
        return getAllRolesService.GetAllRolesAsync(pageRequest, cancellationToken);
    }

    public Task<Result<RoleEntity>> GetRoleByNameAsync(string roleName, CancellationToken cancellationToken = default)
    {
        return getRoleByNameService.GetRoleByNameAsync(roleName, cancellationToken);
    }

    public Task<Result> UpdateRoleAsync(Guid roleId, string newRoleName, CancellationToken cancellationToken = default)
    {
        return updateRoleService.UpdateRoleAsync(roleId, newRoleName, cancellationToken);
    }

    public Task<Result> DeleteRoleByNameAsync(string roleName, CancellationToken cancellationToken = default)
    {
        return deleteRoleByNameService.DeleteRoleByNameAsync(roleName, cancellationToken);
    }
}