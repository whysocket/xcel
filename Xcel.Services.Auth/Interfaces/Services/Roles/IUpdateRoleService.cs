using Domain.Results;

namespace Xcel.Services.Auth.Interfaces.Services.Roles;

internal interface IUpdateRoleService
{
    Task<Result> UpdateRoleAsync(Guid roleId, string newRoleName, CancellationToken cancellationToken = default);
}