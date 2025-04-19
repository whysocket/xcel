using Domain.Results;
using Microsoft.Extensions.Logging;
using Xcel.Services.Auth.Interfaces.Repositories;
using Xcel.Services.Auth.Interfaces.Services.Roles;

namespace Xcel.Services.Auth.Implementations.Services.Roles;

internal sealed class UpdateRoleService(IRolesRepository rolesRepository, ILogger<UpdateRoleService> logger) : IUpdateRoleService
{
    private const string ServiceName = "[UpdateRoleService]";

    public async Task<Result> UpdateRoleAsync(Guid roleId, string newRoleName, CancellationToken cancellationToken = default)
    {
        if (roleId == Guid.Empty)
        {
            logger.LogWarning($"{ServiceName} - Validation failed: Invalid roleId.");
            return Result.Fail(new Error(ErrorType.Validation, "Invalid roleId"));
        }

        var existingRole = await rolesRepository.GetByIdAsync(roleId, cancellationToken);
        if (existingRole is null)
        {
            logger.LogWarning($"{ServiceName} - Not Found: Role with id '{roleId}' not found.");
            return Result.Fail(new Error(ErrorType.NotFound, $"The role with id '{roleId}' is not found."));
        }

        var existingRoleName = await rolesRepository.GetByNameInsensitiveAsync(newRoleName, cancellationToken);
        if (existingRoleName is not null && existingRoleName.Id != roleId)
        {
            logger.LogWarning($"{ServiceName} - Conflict: Role with name '{newRoleName}' already exists (Id: {existingRoleName.Id}).");
            return Result.Fail(new Error(ErrorType.Conflict, $"The role '{newRoleName}' already exists."));
        }

        if (string.IsNullOrWhiteSpace(newRoleName))
        {
            logger.LogWarning($"{ServiceName} - Validation failed: The new role name is required.");
            return Result.Fail(new Error(ErrorType.Validation, "The new role name is required"));
        }

        existingRole.Name = newRoleName;

        rolesRepository.Update(existingRole);
        await rolesRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation($"{ServiceName} - Role updated successfully. RoleId: {roleId}, New Name: {newRoleName}.");
        return Result.Ok();
    }
}