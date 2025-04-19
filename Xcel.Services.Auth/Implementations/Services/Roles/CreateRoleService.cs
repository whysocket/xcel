using Domain.Results;
using Microsoft.Extensions.Logging;
using Xcel.Services.Auth.Interfaces.Repositories;
using Xcel.Services.Auth.Interfaces.Services.Roles;
using Xcel.Services.Auth.Models;

namespace Xcel.Services.Auth.Implementations.Services.Roles;

internal sealed class CreateRoleService(IRolesRepository rolesRepository, ILogger<CreateRoleService> logger) : ICreateRoleService
{
    private const string ServiceName = "[CreateRoleService]";

    public async Task<Result<RoleEntity>> CreateRoleAsync(string roleName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(roleName))
        {
            logger.LogWarning($"{ServiceName} - Validation failed: Role name is required.");
            return Result.Fail<RoleEntity>(new Error(ErrorType.Validation, "The role name is required"));
        }

        var existingRole = await rolesRepository.GetByNameInsensitiveAsync(roleName, cancellationToken);
        if (existingRole is not null)
        {
            logger.LogWarning($"{ServiceName} - Conflict: Role with name '{roleName}' already exists (Id: {existingRole.Id}).");
            return Result.Fail<RoleEntity>(new Error(ErrorType.Conflict, $"The role '{roleName}' already exists."));
        }

        var newRole = new RoleEntity { Id = Guid.NewGuid(), Name = roleName };

        await rolesRepository.AddAsync(newRole, cancellationToken);
        await rolesRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation($"{ServiceName} - Role created successfully. RoleId: {newRole.Id}, Name: {newRole.Name}.");
        return Result.Ok(newRole);
    }
}