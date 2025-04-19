using Domain.Interfaces.Repositories.Shared;
using Domain.Results;
using Xcel.Services.Auth.Interfaces.Services.Roles;
using Xcel.Services.Auth.Interfaces.Services.Roles.Facade;
using Xcel.Services.Auth.Models;

namespace Xcel.Services.Auth.Implementations.Services.Roles.Facade;

internal sealed class RoleService(
    ICreateRoleCommand createRoleCommand,
    IGetAllRolesQuery getAllRolesQuery,
    IGetRoleByNameQuery getRoleByNameQuery,
    IUpdateRoleCommand updateRoleCommand,
    IDeleteRoleByNameCommand deleteRoleByNameCommand)
    : IRoleService
{
    public Task<Result<RoleEntity>> CreateRoleAsync(string roleName, CancellationToken cancellationToken = default) 
        => createRoleCommand.CreateRoleAsync(roleName, cancellationToken);

    public Task<Result<PageResult<RoleEntity>>> GetAllRolesAsync(PageRequest pageRequest, CancellationToken cancellationToken = default) 
        => getAllRolesQuery.GetAllRolesAsync(pageRequest, cancellationToken);

    public Task<Result<RoleEntity>> GetRoleByNameAsync(string roleName, CancellationToken cancellationToken = default) 
        => getRoleByNameQuery.GetRoleByNameAsync(roleName, cancellationToken);

    public Task<Result> UpdateRoleAsync(Guid roleId, string newRoleName, CancellationToken cancellationToken = default) 
        => updateRoleCommand.UpdateRoleAsync(roleId, newRoleName, cancellationToken);

    public Task<Result> DeleteRoleByNameAsync(string roleName, CancellationToken cancellationToken = default) 
        => deleteRoleByNameCommand.DeleteRoleByNameAsync(roleName, cancellationToken);
}