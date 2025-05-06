using Domain.Results;

namespace Xcel.Services.Auth.Features.RefreshTokens.Commands.Interfaces;

internal interface IRevokeRefreshTokenCommand
{
    Task<Result> ExecuteAsync(string token, CancellationToken cancellationToken = default);
}
