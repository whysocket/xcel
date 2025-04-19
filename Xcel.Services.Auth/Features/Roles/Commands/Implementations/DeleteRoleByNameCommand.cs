using Domain.Results;
using Microsoft.Extensions.Logging;
using Xcel.Services.Auth.Features.Roles.Commands.Interfaces;
using Xcel.Services.Auth.Interfaces.Repositories;

namespace Xcel.Services.Auth.Features.Roles.Commands.Implementations;

internal static class DeleteRoleByNameServiceErrors
{
    internal static Error RoleNotFound(string roleName) => new(ErrorType.NotFound, $"The role '{roleName}' is not found.");
}

internal sealed class DeleteRoleByNameCommand(IRolesRepository rolesRepository, ILogger<DeleteRoleByNameCommand> logger) : IDeleteRoleByNameCommand
{
    private const string ServiceName = "[DeleteRoleByNameCommand]";

    public async Task<Result> ExecuteAsync(string roleName, CancellationToken cancellationToken = default)
    {
        roleName = roleName.ToLowerInvariant();
        var existingRole = await rolesRepository.GetByNameInsensitiveAsync(roleName, cancellationToken);
        if (existingRole is null)
        {
            logger.LogWarning($"{ServiceName} - Not Found: Role '{roleName}' not found for deletion.");
            return Result.Fail(DeleteRoleByNameServiceErrors.RoleNotFound(roleName));
        }

        rolesRepository.Remove(existingRole);
        await rolesRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation($"{ServiceName} - Role deleted successfully. RoleId: {existingRole.Id}, Name: {existingRole.Name}.");
        return Result.Ok();
    }
}