using Domain.Interfaces.Repositories;
using Domain.Results;
using Microsoft.Extensions.Logging;
using Xcel.Services.Auth.Interfaces.Services;
using Xcel.Services.Auth.Interfaces.Services.Authentication;
using Xcel.Services.Auth.Interfaces.Services.Jwt.Facade;
using Xcel.Services.Auth.Interfaces.Services.RefreshTokens.Facade;
using Xcel.Services.Auth.Models;

namespace Xcel.Services.Auth.Implementations.Services.Authentication;

internal static class RefreshTokenExchangeServiceErrors
{
    internal static readonly Error PersonNotFound =
        new(ErrorType.NotFound, "The person associated with this token was not found.");
}

internal sealed class RefreshTokenExchangeCommand(
    IClientInfoService clientInfoService,
    IPersonsRepository personRepository,
    IRefreshTokenService refreshTokenService,
    IJwtTokenService jwtTokenService,
    ILogger<RefreshTokenExchangeCommand> logger)
    : IRefreshTokenExchangeCommand
{
    private const string ServiceName = "[RefreshTokenExchangeCommand]";

    public async Task<Result<AuthTokens>> RefreshTokenAsync(string refreshTokenValue, CancellationToken cancellationToken = default)
    {
        logger.LogInformation($"{ServiceName} - Validating refresh token");

        var validateResult = await refreshTokenService.ValidateRefreshTokenAsync(refreshTokenValue, cancellationToken);
        if (validateResult.IsFailure)
        {
            logger.LogWarning($"{ServiceName} - Validation failed");
            return Result.Fail<AuthTokens>(validateResult.Errors);
        }

        var revokeResult = await refreshTokenService.RevokeRefreshTokenAsync(refreshTokenValue, cancellationToken);
        if (revokeResult.IsFailure)
        {
            logger.LogWarning($"{ServiceName} - Revoke failed");
            return Result.Fail<AuthTokens>(revokeResult.Errors);
        }

        var person = await personRepository.GetByIdAsync(clientInfoService.PersonId, cancellationToken);
        if (person is null)
        {
            logger.LogWarning($"{ServiceName} - No person found for token");
            return Result.Fail<AuthTokens>(RefreshTokenExchangeServiceErrors.PersonNotFound);
        }

        var jwtResult = await jwtTokenService.GenerateAsync(person, cancellationToken);
        if (jwtResult.IsFailure)
        {
            return Result.Fail<AuthTokens>(jwtResult.Errors);
        }

        var refreshTokenResult = await refreshTokenService.GenerateRefreshTokenAsync(cancellationToken);
        if (refreshTokenResult.IsFailure)
        {
            return Result.Fail<AuthTokens>(refreshTokenResult.Errors);
        }

        logger.LogInformation($"{ServiceName} - Token refresh succeeded");
        return Result.Ok(new AuthTokens(jwtResult.Value, refreshTokenResult.Value.Token));
    }
}