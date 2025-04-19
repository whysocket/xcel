using Domain.Interfaces.Repositories.Shared;
using Domain.Results;
using Xcel.Services.Auth.Models;

namespace Xcel.Services.Auth.Interfaces.Services.Roles;

internal interface IGetAllRolesQuery
{
    Task<Result<PageResult<RoleEntity>>> GetAllRolesAsync(PageRequest pageRequest, CancellationToken cancellationToken = default);
}