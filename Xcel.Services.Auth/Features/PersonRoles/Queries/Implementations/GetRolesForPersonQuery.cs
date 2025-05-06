using Domain.Results;
using Microsoft.Extensions.Logging;
using Xcel.Services.Auth.Features.PersonRoles.Queries.Interfaces;
using Xcel.Services.Auth.Interfaces.Repositories;
using Xcel.Services.Auth.Models;

namespace Xcel.Services.Auth.Features.PersonRoles.Queries.Implementations;

internal sealed class GetRolesForPersonQuery(
    IPersonRoleRepository personRoleRepository,
    ILogger<GetRolesForPersonQuery> logger
) : IGetRolesForPersonQuery
{
    private const string ServiceName = "[GetRolesForPersonQuery]";

    public async Task<Result<List<PersonRoleEntity>>> ExecuteAsync(
        Guid personId,
        CancellationToken cancellationToken = default
    )
    {
        if (personId == Guid.Empty)
        {
            logger.LogWarning($"{ServiceName} - Validation failed: {nameof(personId)} is invalid.");
            return Result.Fail<List<PersonRoleEntity>>(CommonErrors.InvalidGuid(nameof(personId)));
        }

        var personRolesResult = await personRoleRepository.GetRolesForPersonAsync(
            personId,
            new(1, 100),
            cancellationToken
        );

        logger.LogInformation(
            $"{ServiceName} - Retrieved roles for person. personId: {personId}, roleCount: {personRolesResult.TotalCount}."
        );
        return Result.Ok(personRolesResult.Items);
    }
}
