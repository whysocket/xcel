using Domain.Results;
using Xcel.Services.Auth.Models;

namespace Xcel.Services.Auth.Features.Authentication.Commands.Interfaces;

internal interface IExchangeRefreshTokenCommand
{
    Task<Result<AuthTokens>> ExecuteAsync(
        string refreshTokenValue,
        CancellationToken cancellationToken = default
    );
}
