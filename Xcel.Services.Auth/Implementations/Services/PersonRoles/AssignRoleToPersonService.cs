using Domain.Results;
using Microsoft.Extensions.Logging;
using Xcel.Services.Auth.Interfaces.Repositories;
using Xcel.Services.Auth.Interfaces.Services.PersonRoles;
using Xcel.Services.Auth.Models;

namespace Xcel.Services.Auth.Implementations.Services.PersonRoles;

internal static class AssignRoleToPersonServiceErrors
{
    internal static Error RoleNotFound(Guid roleId) => new(ErrorType.NotFound, $"Role with ID '{roleId}' not found.");
    internal static Error RoleAlreadyAssigned() => new(ErrorType.Conflict, "This role is already assigned to the person.");
}

internal sealed class AssignRoleToPersonService(
    IPersonRoleRepository personRoleRepository,
    IRolesRepository rolesRepository,
    ILogger<AssignRoleToPersonService> logger) : IAssignRoleToPersonService
{
    private const string ServiceName = "[AssignRoleToPersonService]";

    public async Task<Result> AssignRoleToPersonAsync(
        Guid personId,
        Guid roleId,
        CancellationToken cancellationToken = default)
    {
        if (personId == Guid.Empty)
        {
            logger.LogWarning($"{ServiceName} - Validation failed: personId is invalid.");
            return Result.Fail(CommonErrors.InvalidGuid(nameof(personId)));
        }

        if (roleId == Guid.Empty)
        {
            logger.LogWarning($"{ServiceName} - Validation failed: roleId is invalid.");
            return Result.Fail(CommonErrors.InvalidGuid(nameof(roleId)));
        }

        var roleExists = await rolesRepository.GetByIdAsync(roleId, cancellationToken);
        if (roleExists is null)
        {
            logger.LogWarning($"{ServiceName} - Not Found: Role with ID '{roleId}' not found.");
            return Result.Fail(AssignRoleToPersonServiceErrors.RoleNotFound(roleId));
        }

        var existingPersonRole = await personRoleRepository.GetPersonRoleAsync(personId, roleId, cancellationToken);
        if (existingPersonRole is not null)
        {
            logger.LogWarning($"{ServiceName} - Conflict: This role is already assigned to the person.");
            return Result.Fail(AssignRoleToPersonServiceErrors.RoleAlreadyAssigned());
        }

        var personRole = new PersonRoleEntity
        {
            PersonId = personId,
            RoleId = roleId
        };

        await personRoleRepository.AddAsync(personRole, cancellationToken);
        await personRoleRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation($"{ServiceName} - Role assigned to person. personId: {personId}, roleId: {roleId}.");
        return Result.Ok();
    }
}