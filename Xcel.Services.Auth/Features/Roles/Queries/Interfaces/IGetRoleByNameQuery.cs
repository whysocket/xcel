using Domain.Results;
using Xcel.Services.Auth.Models;

namespace Xcel.Services.Auth.Features.Roles.Queries.Interfaces;

internal interface IGetRoleByNameQuery
{
    Task<Result<RoleEntity>> ExecuteAsync(string roleName, CancellationToken cancellationToken = default);
}