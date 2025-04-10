using Domain.Results;
using Xcel.Services.Auth.Interfaces.Repositories;
using Xcel.Services.Auth.Interfaces.Services;
using Xcel.Services.Auth.Models;

namespace Xcel.Services.Auth.Implementations.Services;

internal sealed class PersonRoleService(IPersonRoleRepository personRoleRepository, IRolesRepository rolesRepository) : IPersonRoleService
{
    public async Task<Result> AddRoleToPersonAsync(Guid personId, Guid roleId, CancellationToken cancellationToken = default)
    {
        if (personId == Guid.Empty || roleId == Guid.Empty)
        {
            return Result.Fail(new Error(ErrorType.Validation, "PersonId and RoleId must be valid GUIDs."));
        }

        var roleExists = await rolesRepository.GetByIdAsync(roleId, cancellationToken);
        if (roleExists is null)
        {
            return Result.Fail(new Error(ErrorType.NotFound, $"Role with ID '{roleId}' not found."));
        }

        var existingPersonRole = await personRoleRepository.GetPersonRoleAsync(personId, roleId, cancellationToken);
        if (existingPersonRole is not null)
        {
            return Result.Fail(new Error(ErrorType.Conflict, "This role is already assigned to the person."));
        }

        var personRole = new PersonRoleEntity
        {
            PersonId = personId,
            RoleId = roleId
        };

        await personRoleRepository.AddAsync(personRole, cancellationToken);
        await personRoleRepository.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }

    public async Task<Result<List<RoleEntity>>> GetRolesForPersonAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        if (personId == Guid.Empty)
        {
            return Result.Fail<List<RoleEntity>>(new Error(ErrorType.Validation, "PersonId must be a valid GUID."));
        }

        var personRoles = await personRoleRepository.GetRolesForPersonAsync(personId, cancellationToken);

        return Result.Ok(personRoles);
    }

    public async Task<Result> RemoveRoleFromPersonAsync(Guid personId, Guid roleId, CancellationToken cancellationToken = default)
    {
        if (personId == Guid.Empty || roleId == Guid.Empty)
        {
            return Result.Fail(new Error(ErrorType.Validation, "PersonId and RoleId must be valid GUIDs."));
        }

        var personRole = await personRoleRepository.GetPersonRoleAsync(personId, roleId, cancellationToken);
        if (personRole is null)
        {
            return Result.Fail(new Error(ErrorType.NotFound, "Role assignment not found for the person."));
        }

        personRoleRepository.Remove(personRole);
        await personRoleRepository.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}