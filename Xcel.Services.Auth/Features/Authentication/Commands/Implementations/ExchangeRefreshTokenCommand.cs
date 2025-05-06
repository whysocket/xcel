using Domain.Interfaces.Repositories;
using Domain.Results;
using Microsoft.Extensions.Logging;
using Xcel.Services.Auth.Features.Authentication.Commands.Interfaces;
using Xcel.Services.Auth.Features.Jwt.Commands.Interfaces;
using Xcel.Services.Auth.Features.RefreshTokens.Commands.Interfaces;
using Xcel.Services.Auth.Interfaces.Services;
using Xcel.Services.Auth.Models;

namespace Xcel.Services.Auth.Features.Authentication.Commands.Implementations;

internal static class RefreshTokenExchangeServiceErrors
{
    internal static readonly Error PersonNotFound = new(
        ErrorType.NotFound,
        "The person associated with this token was not found."
    );
}

internal sealed class ExchangeRefreshTokenCommand(
    IClientInfoService clientInfoService,
    IPersonsRepository personRepository,
    IGenerateRefreshTokenCommand generateRefreshTokenCommand,
    IRevokeRefreshTokenCommand revokeRefreshTokenCommand,
    IGenerateJwtTokenCommand generateJwtTokenCommand,
    ILogger<ExchangeRefreshTokenCommand> logger
) : IExchangeRefreshTokenCommand
{
    private const string ServiceName = "[ExchangeRefreshTokenCommand]";

    public async Task<Result<AuthTokens>> ExecuteAsync(
        string refreshTokenValue,
        CancellationToken cancellationToken = default
    )
    {
        logger.LogInformation($"{ServiceName} - Validating refresh token");

        var validateResult = await revokeRefreshTokenCommand.ExecuteAsync(
            refreshTokenValue,
            cancellationToken
        );
        if (validateResult.IsFailure)
        {
            logger.LogWarning($"{ServiceName} - Validation failed");
            return Result.Fail<AuthTokens>(validateResult.Errors);
        }

        var revokeResult = await revokeRefreshTokenCommand.ExecuteAsync(
            refreshTokenValue,
            cancellationToken
        );
        if (revokeResult.IsFailure)
        {
            logger.LogWarning($"{ServiceName} - Revoke failed");
            return Result.Fail<AuthTokens>(revokeResult.Errors);
        }

        var user = await personRepository.GetByIdAsync(clientInfoService.UserId, cancellationToken);
        if (user is null)
        {
            logger.LogWarning($"{ServiceName} - No user found for token");
            return Result.Fail<AuthTokens>(RefreshTokenExchangeServiceErrors.PersonNotFound);
        }

        var jwtResult = await generateJwtTokenCommand.ExecuteAsync(user, cancellationToken);
        if (jwtResult.IsFailure)
        {
            return Result.Fail<AuthTokens>(jwtResult.Errors);
        }

        var refreshTokenResult = await generateRefreshTokenCommand.ExecuteAsync(
            user.Id,
            cancellationToken
        );
        if (refreshTokenResult.IsFailure)
        {
            return Result.Fail<AuthTokens>(refreshTokenResult.Errors);
        }

        logger.LogInformation($"{ServiceName} - Token refresh succeeded");
        return Result.Ok(new AuthTokens(jwtResult.Value, refreshTokenResult.Value.Token));
    }
}
