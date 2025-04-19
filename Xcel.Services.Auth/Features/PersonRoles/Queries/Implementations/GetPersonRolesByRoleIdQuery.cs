using Domain.Interfaces.Repositories.Shared;
using Domain.Results;
using Microsoft.Extensions.Logging;
using Xcel.Services.Auth.Features.PersonRoles.Queries.Interfaces;
using Xcel.Services.Auth.Interfaces.Repositories;
using Xcel.Services.Auth.Models;

namespace Xcel.Services.Auth.Features.PersonRoles.Queries.Implementations;

internal sealed class GetPersonRolesByRoleIdQuery(
    IPersonRoleRepository personRoleRepository,
    ILogger<GetPersonRolesByRoleIdQuery> logger) : IGetPersonRolesByRoleIdQuery
{
    private const string ServiceName = "[GetPersonRolesByRoleIdQuery]";

    public async Task<Result<PageResult<PersonRoleEntity>>> ExecuteAsync(
        Guid roleId,
        PageRequest pageRequest,
        CancellationToken cancellationToken = default)
    {
        if (roleId == Guid.Empty)
        {
            logger.LogWarning($"{ServiceName} - Validation failed: {nameof(roleId)} is invalid.");
            return Result.Fail<PageResult<PersonRoleEntity>>(CommonErrors.InvalidGuid(nameof(roleId)));
        }

        var personsRoles = await personRoleRepository.GetAllPersonsRolesByRoleIdAsync(
            roleId,
            pageRequest,
            cancellationToken);

        logger.LogInformation($"{ServiceName} - Retrieved person-role assignments by role. roleId: {roleId}, page: {pageRequest.PageNumber}, pageSize: {pageRequest.PageSize}, totalCount: {personsRoles.TotalCount}.");

        return Result.Ok(personsRoles);
    }
}