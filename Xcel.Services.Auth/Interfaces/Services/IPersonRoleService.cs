using Domain.Results;
using Xcel.Services.Auth.Models;

namespace Xcel.Services.Auth.Interfaces.Services;

public interface IPersonRoleService
{
    Task<Result> AddRoleToPersonAsync(Guid personId, Guid roleId, CancellationToken cancellationToken = default);
    Task<Result<List<RoleEntity>>> GetRolesForPersonAsync(Guid personId, CancellationToken cancellationToken = default);
    Task<Result> RemoveRoleFromPersonAsync(Guid personId, Guid roleId, CancellationToken cancellationToken = default);
}