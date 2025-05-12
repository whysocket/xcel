using Domain.Results;
using Xcel.Services.Auth.Models;

namespace Xcel.Services.Auth.Features.RefreshTokens.Commands.Interfaces;

internal interface IRevokeRefreshTokenCommand
{
    Task<Result<RefreshTokenEntity>> ExecuteAsync(string token, CancellationToken cancellationToken = default);
}
