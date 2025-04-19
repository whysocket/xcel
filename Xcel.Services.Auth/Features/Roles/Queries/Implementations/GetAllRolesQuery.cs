using Domain.Interfaces.Repositories.Shared;
using Domain.Results;
using Microsoft.Extensions.Logging;
using Xcel.Services.Auth.Features.Roles.Queries.Interfaces;
using Xcel.Services.Auth.Interfaces.Repositories;
using Xcel.Services.Auth.Models;

namespace Xcel.Services.Auth.Features.Roles.Queries.Implementations;

internal sealed class GetAllRolesQuery(IRolesRepository rolesRepository, ILogger<GetAllRolesQuery> logger) : IGetAllRolesQuery
{
    private const string ServiceName = "[GetAllRolesQuery]";

    public async Task<Result<PageResult<RoleEntity>>> ExecuteAsync(PageRequest pageRequest,
        CancellationToken cancellationToken = default)
    {
        var allRoles = await rolesRepository.GetAllAsync(pageRequest, r => r.Name, cancellationToken);
        logger.LogInformation($"{ServiceName} - Retrieved all roles. Page: {pageRequest.PageNumber}, PageSize: {pageRequest.PageSize}, TotalCount: {allRoles.TotalCount}.");
        
        return Result.Ok(allRoles);
    }
}