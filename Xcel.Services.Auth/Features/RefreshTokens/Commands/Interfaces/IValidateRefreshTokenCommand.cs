using Domain.Results;

namespace Xcel.Services.Auth.Features.RefreshTokens.Commands.Interfaces;

internal interface IValidateRefreshTokenCommand
{
    Task<Result> ExecuteAsync(string refreshToken, CancellationToken cancellationToken = default);
}
