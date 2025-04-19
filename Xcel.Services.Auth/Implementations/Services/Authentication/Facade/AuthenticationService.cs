using Domain.Results;
using Xcel.Services.Auth.Interfaces.Services.Authentication;
using Xcel.Services.Auth.Interfaces.Services.Authentication.Facade;
using Xcel.Services.Auth.Models;

namespace Xcel.Services.Auth.Implementations.Services.Authentication.Facade;

internal sealed class AuthenticationService(
    IRequestOtpByEmailCommand requestOtpByEmailCommand,
    ILoginWithOtpCommand loginWithOtpCommand,
    IRefreshTokenExchangeCommand refreshTokenExchangeCommand)
    : IAuthenticationService
{
    public Task<Result> RequestOtpByEmailAsync(string emailAddress, CancellationToken cancellationToken = default) =>
        requestOtpByEmailCommand.RequestOtpByEmailAsync(emailAddress, cancellationToken);

    public Task<Result<AuthTokens>> LoginWithOtpAsync(string email, string otp,
        CancellationToken cancellationToken = default) =>
        loginWithOtpCommand.LoginWithOtpAsync(email, otp, cancellationToken);

    public Task<Result<AuthTokens>> RefreshTokenAsync(string refreshTokenValue,
        CancellationToken cancellationToken = default) =>
        refreshTokenExchangeCommand.RefreshTokenAsync(refreshTokenValue, cancellationToken);
}