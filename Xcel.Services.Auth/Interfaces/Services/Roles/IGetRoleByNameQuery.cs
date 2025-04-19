using Domain.Results;
using Xcel.Services.Auth.Models;

namespace Xcel.Services.Auth.Interfaces.Services.Roles;

internal interface IGetRoleByNameQuery
{
    Task<Result<RoleEntity>> GetRoleByNameAsync(string roleName, CancellationToken cancellationToken = default);
}