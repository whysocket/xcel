using Domain.Results;

namespace Xcel.Services.Auth.Interfaces.Services.PersonRoles;

internal interface IUnassignRoleFromPersonService
{
    Task<Result> UnassignRoleFromPersonAsync(Guid personId, Guid roleId, CancellationToken cancellationToken = default);
}