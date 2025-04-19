using Domain.Results;

namespace Xcel.Services.Auth.Features.PersonRoles.Commands.Interfaces;

internal interface IUnassignRoleFromPersonCommand
{
    Task<Result> ExecuteAsync(Guid personId, Guid roleId, CancellationToken cancellationToken = default);
}