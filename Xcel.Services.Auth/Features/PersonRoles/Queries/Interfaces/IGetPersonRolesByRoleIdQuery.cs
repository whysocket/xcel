using Domain.Interfaces.Repositories.Shared;
using Domain.Results;
using Xcel.Services.Auth.Models;

namespace Xcel.Services.Auth.Features.PersonRoles.Queries.Interfaces;

internal interface IGetPersonRolesByRoleIdQuery
{
    Task<Result<PageResult<PersonRoleEntity>>> ExecuteAsync(
        Guid roleId,
        PageRequest pageRequest,
        CancellationToken cancellationToken = default
    );
}
