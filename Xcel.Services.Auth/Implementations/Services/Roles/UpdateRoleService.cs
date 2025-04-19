using Domain.Results;
using Microsoft.Extensions.Logging;
using Xcel.Services.Auth.Interfaces.Repositories;
using Xcel.Services.Auth.Interfaces.Services.Roles;

namespace Xcel.Services.Auth.Implementations.Services.Roles;

internal static class UpdateRoleServiceErrors
{
    internal static Error InvalidRoleId() => new(ErrorType.Validation, "Invalid roleId");
    internal static Error RoleNotFound(Guid roleId) => new(ErrorType.NotFound, $"The role with id '{roleId}' is not found.");
    internal static Error RoleNameConflict(string roleName) => new(ErrorType.Conflict, $"The role '{roleName}' already exists.");
    internal static Error RoleNameRequired() => new(ErrorType.Validation, "The new role name is required");
}

internal sealed class UpdateRoleService(IRolesRepository rolesRepository, ILogger<UpdateRoleService> logger) : IUpdateRoleService
{
    private const string ServiceName = "[UpdateRoleService]";

    public async Task<Result> UpdateRoleAsync(Guid roleId, string newRoleName, CancellationToken cancellationToken = default)
    {
        if (roleId == Guid.Empty)
        {
            logger.LogWarning($"{ServiceName} - Validation failed: Invalid roleId.");
            return Result.Fail(UpdateRoleServiceErrors.InvalidRoleId());
        }

        var existingRole = await rolesRepository.GetByIdAsync(roleId, cancellationToken);
        if (existingRole is null)
        {
            logger.LogWarning($"{ServiceName} - Not Found: Role with id '{roleId}' not found.");
            return Result.Fail(UpdateRoleServiceErrors.RoleNotFound(roleId));
        }

        var existingRoleName = await rolesRepository.GetByNameInsensitiveAsync(newRoleName, cancellationToken);
        if (existingRoleName is not null && existingRoleName.Id != roleId)
        {
            logger.LogWarning($"{ServiceName} - Conflict: Role with name '{newRoleName}' already exists (Id: {existingRoleName.Id}).");
            return Result.Fail(UpdateRoleServiceErrors.RoleNameConflict(newRoleName));
        }

        if (string.IsNullOrWhiteSpace(newRoleName))
        {
            logger.LogWarning($"{ServiceName} - Validation failed: The new role name is required.");
            return Result.Fail(UpdateRoleServiceErrors.RoleNameRequired());
        }

        existingRole.Name = newRoleName;

        rolesRepository.Update(existingRole);
        await rolesRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation($"{ServiceName} - Role updated successfully. RoleId: {roleId}, New Name: {newRoleName}.");
        return Result.Ok();
    }
}