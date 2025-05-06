using Domain.Results;
using Microsoft.Extensions.Logging;
using Xcel.Services.Auth.Features.PersonRoles.Commands.Interfaces;
using Xcel.Services.Auth.Interfaces.Repositories;

namespace Xcel.Services.Auth.Features.PersonRoles.Commands.Implementations;

internal static class UnassignRoleFromPersonServiceErrors
{
    internal static Error RoleAssignmentNotFound() =>
        new(ErrorType.NotFound, "Role assignment not found for the person.");
}

internal sealed class UnassignRoleFromPersonCommand(
    IPersonRoleRepository personRoleRepository,
    ILogger<UnassignRoleFromPersonCommand> logger
) : IUnassignRoleFromPersonCommand
{
    private const string ServiceName = "[UnassignRoleFromPersonCommand]";

    public async Task<Result> ExecuteAsync(
        Guid personId,
        Guid roleId,
        CancellationToken cancellationToken = default
    )
    {
        if (personId == Guid.Empty)
        {
            logger.LogWarning($"{ServiceName} - Validation failed: {nameof(personId)} is invalid.");
            return Result.Fail(CommonErrors.InvalidGuid(nameof(personId)));
        }

        if (roleId == Guid.Empty)
        {
            logger.LogWarning($"{ServiceName} - Validation failed: {nameof(roleId)} is invalid.");
            return Result.Fail(CommonErrors.InvalidGuid(nameof(roleId)));
        }

        var personRole = await personRoleRepository.GetPersonRoleAsync(
            personId,
            roleId,
            cancellationToken
        );
        if (personRole is null)
        {
            logger.LogWarning(
                $"{ServiceName} - Not Found: Role assignment not found for the person."
            );
            return Result.Fail(UnassignRoleFromPersonServiceErrors.RoleAssignmentNotFound());
        }

        personRoleRepository.Remove(personRole);
        await personRoleRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            $"{ServiceName} - Role unassigned from person. personId: {personId}, roleId: {roleId}."
        );
        return Result.Ok();
    }
}
