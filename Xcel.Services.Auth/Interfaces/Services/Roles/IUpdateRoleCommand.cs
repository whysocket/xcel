using Domain.Results;

namespace Xcel.Services.Auth.Interfaces.Services.Roles;

internal interface IUpdateRoleCommand
{
    Task<Result> UpdateRoleAsync(Guid roleId, string newRoleName, CancellationToken cancellationToken = default);
}