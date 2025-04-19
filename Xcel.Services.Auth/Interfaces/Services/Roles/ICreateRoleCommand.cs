using Domain.Results;
using Xcel.Services.Auth.Models;

namespace Xcel.Services.Auth.Interfaces.Services.Roles;

internal interface ICreateRoleCommand
{
    Task<Result<RoleEntity>> CreateRoleAsync(string roleName, CancellationToken cancellationToken = default);
}