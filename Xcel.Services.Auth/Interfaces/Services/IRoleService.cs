using Domain.Interfaces.Repositories.Shared;
using Domain.Results;
using Xcel.Services.Auth.Models;

namespace Xcel.Services.Auth.Interfaces.Services;

internal interface IRoleService
{
    Task<Result<RoleEntity>> CreateRoleAsync(string roleName, CancellationToken cancellationToken = default);

    Task<Result<PageResult<RoleEntity>>> GetAllRolesAsync(
        int page = 1,
        int pageSize = 100,
        CancellationToken cancellationToken = default);
    
    Task<Result<RoleEntity>> GetRoleByNameAsync(string roleName, CancellationToken cancellationToken = default);

    Task<Result> UpdateRoleAsync(Guid roleId, string newRoleName, CancellationToken cancellationToken = default);

    Task<Result> DeleteRoleByNameAsync(string roleName, CancellationToken cancellationToken = default);
}