using Domain.Results;

namespace Xcel.Services.Auth.Interfaces.Services.Roles;

internal interface IDeleteRoleByNameService
{
    Task<Result> DeleteRoleByNameAsync(string roleName, CancellationToken cancellationToken = default);
}