using Domain.Results;
using Microsoft.Extensions.Logging;
using Xcel.Services.Auth.Interfaces.Repositories;
using Xcel.Services.Auth.Interfaces.Services.PersonRoles;
using Xcel.Services.Auth.Models;

namespace Xcel.Services.Auth.Implementations.Services.PersonRoles;

internal sealed class GetRolesForPersonService(
    IPersonRoleRepository personRoleRepository,
    ILogger<GetRolesForPersonService> logger) : IGetRolesForPersonService
{
    private const string ServiceName = "[GetRolesForPersonService]";

    public async Task<Result<List<PersonRoleEntity>>> GetRolesForPersonAsync(
        Guid personId,
        CancellationToken cancellationToken = default)
    {
        if (personId == Guid.Empty)
        {
            logger.LogWarning($"{ServiceName} - Validation failed: {nameof(personId)} is invalid.");
            return Result.Fail<List<PersonRoleEntity>>(CommonErrors.InvalidGuid(nameof(personId)));
        }

        var personRolesResult = await personRoleRepository.GetRolesForPersonAsync(
            personId,
            new(1, 100),
            cancellationToken);

        logger.LogInformation($"{ServiceName} - Retrieved roles for person. personId: {personId}, roleCount: {personRolesResult.TotalCount}.");
        return Result.Ok(personRolesResult.Items);
    }
}