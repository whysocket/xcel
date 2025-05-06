using Domain.Results;
using Xcel.Services.Auth.Models;

namespace Xcel.Services.Auth.Features.PersonRoles.Queries.Interfaces;

internal interface IGetRolesForPersonQuery
{
    Task<Result<List<PersonRoleEntity>>> ExecuteAsync(
        Guid personId,
        CancellationToken cancellationToken = default
    );
}
