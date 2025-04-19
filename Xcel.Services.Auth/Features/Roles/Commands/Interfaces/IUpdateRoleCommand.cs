using Domain.Results;

namespace Xcel.Services.Auth.Features.Roles.Commands.Interfaces;

internal interface IUpdateRoleCommand
{
    Task<Result> ExecuteAsync(Guid roleId, string newRoleName, CancellationToken cancellationToken = default);
}