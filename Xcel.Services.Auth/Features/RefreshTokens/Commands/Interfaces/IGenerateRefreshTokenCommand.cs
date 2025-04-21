using Domain.Results;
using Xcel.Services.Auth.Models;

namespace Xcel.Services.Auth.Features.RefreshTokens.Commands.Interfaces;

internal interface IGenerateRefreshTokenCommand
{
    Task<Result<RefreshTokenEntity>> ExecuteAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}