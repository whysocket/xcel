using Domain.Results;
using Xcel.Services.Auth.Models;

namespace Xcel.Services.Auth.Interfaces.Services.Authentication.Facade;

internal interface IAuthenticationService
{
    Task<Result> RequestOtpByEmailAsync(string emailAddress, CancellationToken cancellationToken = default);
    Task<Result<AuthTokens>> LoginWithOtpAsync(string email, string otp, CancellationToken cancellationToken = default);
    Task<Result<AuthTokens>> RefreshTokenAsync(string refreshTokenValue, CancellationToken cancellationToken = default);
}