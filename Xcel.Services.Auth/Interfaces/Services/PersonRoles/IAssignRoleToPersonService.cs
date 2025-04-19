using Domain.Results;

namespace Xcel.Services.Auth.Interfaces.Services.PersonRoles;

internal  interface IAssignRoleToPersonService
{
    Task<Result> AssignRoleToPersonAsync(Guid personId, Guid roleId, CancellationToken cancellationToken = default);
}