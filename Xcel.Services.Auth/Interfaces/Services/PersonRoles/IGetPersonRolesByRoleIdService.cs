using Domain.Interfaces.Repositories.Shared;
using Domain.Results;
using Xcel.Services.Auth.Models;

namespace Xcel.Services.Auth.Interfaces.Services.PersonRoles;

internal interface IGetPersonRolesByRoleIdService
{
    Task<Result<PageResult<PersonRoleEntity>>> GetPersonRolesByRoleIdAsync(Guid roleId, PageRequest pageRequest, CancellationToken cancellationToken = default);
}