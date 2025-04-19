using Domain.Results;
using Microsoft.Extensions.Logging;
using Xcel.Services.Auth.Interfaces.Repositories;
using Xcel.Services.Auth.Interfaces.Services.Roles;
using Xcel.Services.Auth.Models;

namespace Xcel.Services.Auth.Implementations.Services.Roles;

internal static class GetRoleByNameServiceErrors
{
    internal static Error RoleNotFound(string roleName) => new(ErrorType.NotFound, $"The role with name '{roleName}' is not found.");
}

internal sealed class GetRoleByNameService(IRolesRepository rolesRepository, ILogger<GetRoleByNameService> logger) : IGetRoleByNameService
{
    private const string ServiceName = "[GetRoleByNameService]";

    public async Task<Result<RoleEntity>> GetRoleByNameAsync(string roleName, CancellationToken cancellationToken = default)
    {
        roleName = roleName.ToLowerInvariant();
        var existingRole = await rolesRepository.GetByNameInsensitiveAsync(roleName, cancellationToken);
        if (existingRole is null)
        {
            logger.LogWarning($"{ServiceName} - Not Found: Role with name '{roleName}' not found.");
            return Result.Fail<RoleEntity>(GetRoleByNameServiceErrors.RoleNotFound(roleName));
        }

        logger.LogInformation($"{ServiceName} - Retrieved role by name. Name: {existingRole.Name}, Id: {existingRole.Id}.");
        return Result.Ok(existingRole);
    }
}