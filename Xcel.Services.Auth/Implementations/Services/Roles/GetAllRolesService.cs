using Domain.Interfaces.Repositories.Shared;
using Domain.Results;
using Microsoft.Extensions.Logging;
using Xcel.Services.Auth.Interfaces.Repositories;
using Xcel.Services.Auth.Interfaces.Services.Roles;
using Xcel.Services.Auth.Models;

namespace Xcel.Services.Auth.Implementations.Services.Roles;

internal sealed class GetAllRolesService(IRolesRepository rolesRepository, ILogger<GetAllRolesService> logger) : IGetAllRolesService
{
    private const string ServiceName = "[GetAllRolesService]";

    public async Task<Result<PageResult<RoleEntity>>> GetAllRolesAsync(PageRequest pageRequest, CancellationToken cancellationToken = default)
    {
        var allRoles = await rolesRepository.GetAllAsync(pageRequest, r => r.Name, cancellationToken);
        logger.LogInformation($"{ServiceName} - Retrieved all roles. Page: {pageRequest.PageNumber}, PageSize: {pageRequest.PageSize}, TotalCount: {allRoles.TotalCount}.");
        
        return Result.Ok(allRoles);
    }
}