using Domain.Results;

namespace Xcel.Services.Auth.Features.Roles.Commands.Interfaces;

internal interface IDeleteRoleByNameCommand
{
    Task<Result> ExecuteAsync(string roleName, CancellationToken cancellationToken = default);
}