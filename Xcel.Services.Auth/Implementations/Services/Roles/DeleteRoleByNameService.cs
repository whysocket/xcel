using Domain.Results;
using Microsoft.Extensions.Logging;
using Xcel.Services.Auth.Interfaces.Repositories;
using Xcel.Services.Auth.Interfaces.Services.Roles;

namespace Xcel.Services.Auth.Implementations.Services.Roles;

internal sealed class DeleteRoleByNameService(IRolesRepository rolesRepository, ILogger<DeleteRoleByNameService> logger) : IDeleteRoleByNameService
{
    private const string ServiceName = "[DeleteRoleByNameService]";

    public async Task<Result> DeleteRoleByNameAsync(string roleName, CancellationToken cancellationToken = default)
    {
        roleName = roleName.ToLowerInvariant();
        var existingRole = await rolesRepository.GetByNameInsensitiveAsync(roleName, cancellationToken);
        if (existingRole is null)
        {
            logger.LogWarning($"{ServiceName} - Not Found: Role '{roleName}' not found for deletion.");
            return Result.Fail(new Error(ErrorType.NotFound, $"The role '{roleName}' is not found."));
        }

        rolesRepository.Remove(existingRole);
        await rolesRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation($"{ServiceName} - Role deleted successfully. RoleId: {existingRole.Id}, Name: {existingRole.Name}.");
        return Result.Ok();
    }
}