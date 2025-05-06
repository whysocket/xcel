using Domain.Interfaces.Repositories.Shared;
using Domain.Results;
using Xcel.Services.Auth.Models;

namespace Xcel.Services.Auth.Features.Roles.Queries.Interfaces;

internal interface IGetAllRolesQuery
{
    Task<Result<PageResult<RoleEntity>>> ExecuteAsync(
        PageRequest pageRequest,
        CancellationToken cancellationToken = default
    );
}
