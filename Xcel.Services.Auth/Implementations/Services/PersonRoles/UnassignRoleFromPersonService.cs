using Domain.Results;
using Microsoft.Extensions.Logging;
using Xcel.Services.Auth.Interfaces.Repositories;
using Xcel.Services.Auth.Interfaces.Services.PersonRoles;

namespace Xcel.Services.Auth.Implementations.Services.PersonRoles;

internal static class UnassignRoleFromPersonServiceErrors
{
    internal static Error RoleAssignmentNotFound() => new(ErrorType.NotFound, "Role assignment not found for the person.");
}
internal sealed class UnassignRoleFromPersonService(
    IPersonRoleRepository personRoleRepository,
    ILogger<UnassignRoleFromPersonService> logger) : IUnassignRoleFromPersonService
{
    private const string ServiceName = "[UnassignRoleFromPersonService]";

    public async Task<Result> UnassignRoleFromPersonAsync(Guid personId, Guid roleId,
        CancellationToken cancellationToken = default)
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

        var personRole = await personRoleRepository.GetPersonRoleAsync(personId, roleId, cancellationToken);
        if (personRole is null)
        {
            logger.LogWarning($"{ServiceName} - Not Found: Role assignment not found for the person.");
            return Result.Fail(UnassignRoleFromPersonServiceErrors.RoleAssignmentNotFound());
        }

        personRoleRepository.Remove(personRole);
        await personRoleRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation($"{ServiceName} - Role unassigned from person. personId: {personId}, roleId: {roleId}.");
        return Result.Ok();
    }
}