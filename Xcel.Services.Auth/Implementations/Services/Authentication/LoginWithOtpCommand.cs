using Domain.Interfaces.Repositories;
using Domain.Results;
using Microsoft.Extensions.Logging;
using Xcel.Services.Auth.Interfaces.Services.Authentication;
using Xcel.Services.Auth.Interfaces.Services.Jwt.Facade;
using Xcel.Services.Auth.Interfaces.Services.Otp.Facade;
using Xcel.Services.Auth.Interfaces.Services.RefreshTokens.Facade;
using Xcel.Services.Auth.Models;

namespace Xcel.Services.Auth.Implementations.Services.Authentication;

internal static class LoginWithOtpServiceErrors
{
    internal static Error PersonNotFound(string email) =>
        new(ErrorType.Unauthorized, $"The person with email address '{email}' was not found.");
}

internal sealed class LoginWithOtpCommand(
    IPersonsRepository personRepository,
    IOtpTokenService otpTokenService,
    IJwtTokenService jwtTokenService,
    IRefreshTokenService refreshTokenService,
    ILogger<LoginWithOtpCommand> logger)
    : ILoginWithOtpCommand
{
    private const string ServiceName = "[LoginWithOtpCommand]";

    public async Task<Result<AuthTokens>> LoginWithOtpAsync(string email, string otp, CancellationToken cancellationToken = default)
    {
        logger.LogInformation($"{ServiceName} - Attempting login for email: {email}");

        var person = await personRepository.GetByEmailAsync(email, cancellationToken);
        if (person is null)
        {
            logger.LogWarning($"{ServiceName} - Person not found: {email}");
            return Result.Fail<AuthTokens>(LoginWithOtpServiceErrors.PersonNotFound(email));
        }

        var otpResult = await otpTokenService.ValidateAsync(person, otp, cancellationToken);
        if (otpResult.IsFailure)
        {
            logger.LogWarning($"{ServiceName} - OTP validation failed for: {email}");
            return Result.Fail<AuthTokens>(otpResult.Errors);
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

        logger.LogInformation($"{ServiceName} - Login successful for: {email}");
        return Result.Ok(new AuthTokens(jwtResult.Value, refreshTokenResult.Value.Token));
    }
}