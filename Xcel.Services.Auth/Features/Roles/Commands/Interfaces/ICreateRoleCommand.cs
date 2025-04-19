using Domain.Results;
using Xcel.Services.Auth.Models;

namespace Xcel.Services.Auth.Features.Roles.Commands.Interfaces;

internal interface ICreateRoleCommand
{
    Task<Result<RoleEntity>> ExecuteAsync(string roleName, CancellationToken cancellationToken = default);
}