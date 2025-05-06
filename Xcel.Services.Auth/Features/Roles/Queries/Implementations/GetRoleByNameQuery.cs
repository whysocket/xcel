using Domain.Results;
using Microsoft.Extensions.Logging;
using Xcel.Services.Auth.Features.Roles.Queries.Interfaces;
using Xcel.Services.Auth.Interfaces.Repositories;
using Xcel.Services.Auth.Models;

namespace Xcel.Services.Auth.Features.Roles.Queries.Implementations;

internal static class GetRoleByNameServiceErrors
{
    internal static Error RoleNotFound(string roleName) =>
        new(ErrorType.NotFound, $"The role with name '{roleName}' is not found.");
}

internal sealed class GetRoleByNameQuery(
    IRolesRepository rolesRepository,
    ILogger<GetRoleByNameQuery> logger
) : IGetRoleByNameQuery
{
    private const string ServiceName = "[GetRoleByNameQuery]";

    public async Task<Result<RoleEntity>> ExecuteAsync(
        string roleName,
        CancellationToken cancellationToken = default
    )
    {
        roleName = roleName.ToLowerInvariant();
        var existingRole = await rolesRepository.GetByNameInsensitiveAsync(
            roleName,
            cancellationToken
        );
        if (existingRole is null)
        {
            logger.LogWarning($"{ServiceName} - Not Found: Role with name '{roleName}' not found.");
            return Result.Fail<RoleEntity>(GetRoleByNameServiceErrors.RoleNotFound(roleName));
        }

        logger.LogInformation(
            $"{ServiceName} - Retrieved role by name. Name: {existingRole.Name}, Id: {existingRole.Id}."
        );
        return Result.Ok(existingRole);
    }
}
