using Domain.Interfaces.Repositories;
using Domain.Results;
using Microsoft.Extensions.Logging;
using Xcel.Services.Auth.Features.Authentication.Commands.Interfaces;
using Xcel.Services.Auth.Features.Jwt.Commands.Interfaces;
using Xcel.Services.Auth.Features.Otp.Commands.Interfaces;
using Xcel.Services.Auth.Features.RefreshTokens.Commands.Interfaces;
using Xcel.Services.Auth.Models;

namespace Xcel.Services.Auth.Features.Authentication.Commands.Implementations;

internal static class LoginWithOtpServiceErrors
{
    internal static Error PersonNotFound(string email) =>
        new(ErrorType.Unauthorized, $"The person with email address '{email}' was not found.");
}

internal sealed class LoginWithOtpCommand(
    IPersonsRepository personRepository,
    IValidateOtpCommand validateOtpCommand,
    IGenerateJwtTokenCommand generateJwtTokenCommand,
    IGenerateRefreshTokenCommand generateRefreshTokenCommand,
    ILogger<LoginWithOtpCommand> logger
) : ILoginWithOtpCommand
{
    private const string ServiceName = "[LoginWithOtpCommand]";

    public async Task<Result<AuthTokens>> ExecuteAsync(
        string email,
        string otp,
        CancellationToken cancellationToken = default
    )
    {
        logger.LogInformation($"{ServiceName} - Attempting login for email: {email}");

        var user = await personRepository.GetByEmailAsync(email, cancellationToken);
        if (user is null)
        {
            logger.LogWarning($"{ServiceName} - Person not found: {email}");
            return Result.Fail<AuthTokens>(LoginWithOtpServiceErrors.PersonNotFound(email));
        }

        var otpResult = await validateOtpCommand.ExecuteAsync(user, otp, cancellationToken);
        if (otpResult.IsFailure)
        {
            logger.LogWarning($"{ServiceName} - OTP validation failed for: {email}");
            return Result.Fail<AuthTokens>(otpResult.Errors);
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

        logger.LogInformation($"{ServiceName} - Login successful for: {email}");
        return Result.Ok(new AuthTokens(jwtResult.Value, refreshTokenResult.Value.Token));
    }
}
