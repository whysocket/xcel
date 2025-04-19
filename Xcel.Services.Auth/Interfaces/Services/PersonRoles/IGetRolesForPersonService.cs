using Domain.Results;
using Xcel.Services.Auth.Models;

namespace Xcel.Services.Auth.Interfaces.Services.PersonRoles;

internal interface IGetRolesForPersonService
{
    Task<Result<List<PersonRoleEntity>>> GetRolesForPersonAsync(Guid personId, CancellationToken cancellationToken = default);
}