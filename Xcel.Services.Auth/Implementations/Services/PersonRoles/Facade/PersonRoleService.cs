using Domain.Interfaces.Repositories.Shared;
using Domain.Results;
using Xcel.Services.Auth.Interfaces.Services.PersonRoles;
using Xcel.Services.Auth.Interfaces.Services.PersonRoles.Facade;
using Xcel.Services.Auth.Models;

namespace Xcel.Services.Auth.Implementations.Services.PersonRoles.Facade;

internal sealed class PersonRoleService(
    IAssignRoleToPersonCommand assignRoleToPersonCommand,
    IGetRolesForPersonQuery getRolesForPersonQuery,
    IGetPersonRolesByRoleIdQuery getPersonRolesByRoleIdQuery,
    IUnassignRoleFromPersonCommand unassignRoleFromPersonCommand)
    : IPersonRoleService
{
    public Task<Result> AssignRoleToPersonAsync(Guid personId, Guid roleId, CancellationToken cancellationToken = default)
        => assignRoleToPersonCommand.AssignRoleToPersonAsync(personId, roleId, cancellationToken);

    public Task<Result<List<PersonRoleEntity>>> GetRolesForPersonAsync(Guid personId, CancellationToken cancellationToken = default) 
        => getRolesForPersonQuery.GetRolesForPersonAsync(personId, cancellationToken);

    public Task<Result<PageResult<PersonRoleEntity>>> GetPersonRolesByRoleIdAsync(Guid roleId, PageRequest pageRequest, CancellationToken cancellationToken = default) 
        => getPersonRolesByRoleIdQuery.GetPersonRolesByRoleIdAsync(roleId, pageRequest, cancellationToken);

    public Task<Result> UnassignRoleFromPersonAsync(Guid personId, Guid roleId, CancellationToken cancellationToken = default) 
        => unassignRoleFromPersonCommand.UnassignRoleFromPersonAsync(personId, roleId, cancellationToken);
}