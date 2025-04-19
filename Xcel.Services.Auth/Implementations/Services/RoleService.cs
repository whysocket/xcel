using Domain.Interfaces.Repositories.Shared;
using Domain.Results;
using Xcel.Services.Auth.Interfaces.Repositories;
using Xcel.Services.Auth.Interfaces.Services;
using Xcel.Services.Auth.Models;

namespace Xcel.Services.Auth.Implementations.Services;

internal sealed class RoleService(IRolesRepository rolesRepository) : IRoleService
{
    public async Task<Result<RoleEntity>> CreateRoleAsync(string roleName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(roleName))
        {
            return Result.Fail<RoleEntity>(new Error(ErrorType.Validation, "The role name is required"));
        }

        var existingRole = await rolesRepository.GetByNameInsensitiveAsync(roleName, cancellationToken);
        if (existingRole is not null)
        {
            return Result.Fail<RoleEntity>(new Error(ErrorType.Conflict, $"The role '{roleName}' already exists."));
        }

        var newRole = new RoleEntity
        {
            Id = Guid.NewGuid(),
            Name = roleName
        };

        await rolesRepository.AddAsync(newRole, cancellationToken);
        await rolesRepository.SaveChangesAsync(cancellationToken);

        return Result.Ok(newRole);
    }

    public async Task<Result<PageResult<RoleEntity>>> GetAllRolesAsync(int page = 1, int pageSize = 100, CancellationToken cancellationToken = default)
    {
        var allRoles = await rolesRepository.GetAllAsync(new PageRequest(page, pageSize), cancellationToken);

        return Result.Ok(allRoles);
    }

    public async Task<Result<RoleEntity>> GetRoleByNameAsync(string roleName, CancellationToken cancellationToken = default)
    {
        roleName = roleName.ToLowerInvariant();
        var existingRole = await rolesRepository.GetByNameInsensitiveAsync(roleName, cancellationToken);
        if (existingRole is null)
        {
            return Result.Fail<RoleEntity>(new Error(ErrorType.NotFound, $"The role with name '{roleName}' is not found."));
        }
        
        return Result.Ok(existingRole);
    }

    public async Task<Result> UpdateRoleAsync(Guid roleId, string newRoleName, CancellationToken cancellationToken = default)
    {
        if (roleId == Guid.Empty)
        {
            return Result.Fail(new Error(ErrorType.Validation, "Invalid roleId"));
        }

        var existingRole = await rolesRepository.GetByIdAsync(roleId, cancellationToken);
        if (existingRole is null)
        {
            return Result.Fail(new Error(ErrorType.NotFound, $"The role with id '{roleId}' is not found."));
        }
        
        var existingRoleName = await rolesRepository.GetByNameInsensitiveAsync(newRoleName, cancellationToken);
        if (existingRoleName is not null && existingRoleName.Id != roleId)
        {
            return Result.Fail(new Error(ErrorType.Conflict, $"The role '{newRoleName}' already exists."));
        }

        if (string.IsNullOrWhiteSpace(newRoleName))
        {
            return Result.Fail(new Error(ErrorType.Validation, "The new role name is required"));
        }

        existingRole.Name = newRoleName;

        rolesRepository.Update(existingRole);
        await rolesRepository.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }

    public async Task<Result> DeleteRoleByNameAsync(string roleName, CancellationToken cancellationToken = default)
    {
        var existingRole = await rolesRepository.GetByNameInsensitiveAsync(roleName, cancellationToken);
        if (existingRole is null)
        {
            return Result.Fail(new Error(ErrorType.NotFound, $"The role '{roleName}' is not found."));
        }

        rolesRepository.Remove(existingRole);
        await rolesRepository.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }
}