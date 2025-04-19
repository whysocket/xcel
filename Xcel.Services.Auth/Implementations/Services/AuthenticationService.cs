using Domain.Entities;
using Domain.Interfaces.Repositories;
using Domain.Results;
using Xcel.Services.Auth.Interfaces.Services;
using Xcel.Services.Auth.Interfaces.Services.RefreshTokens.Facade;

namespace Xcel.Services.Auth.Implementations.Services;

internal sealed class AuthenticationService(
    IPersonsRepository personRepository,
    IJwtService jwtService,
    IRefreshTokenService refreshTokenService,
    IClientInfoService clientInfoService,
    IOtpService otpService) : IAuthenticationService
{
    public async Task<Result<AuthTokens>> LoginWithOtpAsync(string email, string otp, CancellationToken cancellationToken = default)
    {
        var existingPerson = await personRepository.GetByEmailAsync(email, cancellationToken);
        if (existingPerson is null)
        {
            return Result.Fail<AuthTokens>(new Error(
                ErrorType.Unauthorized,
                $"The person with email address '{email}' was not found."));
        }

        var existingOtpResult = await otpService.ValidateOtpAsync(
            existingPerson,
            otp,
            cancellationToken);

        if (existingOtpResult.IsFailure)
        {
            return Result.Fail<AuthTokens>(existingOtpResult.Errors);
        }

        return await GenerateAuthTokensAsync(existingPerson, cancellationToken);
    }

    public async Task<Result<AuthTokens>> RefreshTokenAsync(string refreshTokenValue, CancellationToken cancellationToken = default)
    {
        var refreshTokenResult = await refreshTokenService.ValidateRefreshTokenAsync(refreshTokenValue, cancellationToken);
        if (refreshTokenResult.IsFailure)
        {
            return Result.Fail<AuthTokens>(refreshTokenResult.Errors);
        }

        var existingPerson = await personRepository.GetByIdAsync(refreshTokenResult.Value.PersonId, cancellationToken);
        if (existingPerson is null)
        {
            return Result.Fail<AuthTokens>(new Error(
                ErrorType.NotFound,
                "The person associated with this token was not found."));
        }

        return await GenerateAuthTokensAsync(existingPerson, cancellationToken);
    }

    private async Task<Result<AuthTokens>> GenerateAuthTokensAsync(Person person, CancellationToken cancellationToken)
    {
        var jwtResult = await jwtService.GenerateAsync(person, cancellationToken);
        if (jwtResult.IsFailure)
        {
            return Result.Fail<AuthTokens>(jwtResult.Errors);
        }

        var refreshTokenResult = await refreshTokenService.GenerateRefreshTokenAsync(
            person,
            cancellationToken);

        if (refreshTokenResult.IsFailure)
        {
            return Result.Fail<AuthTokens>(refreshTokenResult.Errors);
        }

        return Result.Ok(new AuthTokens(jwtResult.Value, refreshTokenResult.Value.Token));
    }
}