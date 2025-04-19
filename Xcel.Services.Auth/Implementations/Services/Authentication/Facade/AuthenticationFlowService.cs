using Domain.Results;
using Xcel.Services.Auth.Interfaces.Services.Authentication;
using Xcel.Services.Auth.Interfaces.Services.Authentication.Facade;
using Xcel.Services.Auth.Models;

namespace Xcel.Services.Auth.Implementations.Services.Authentication.Facade;

internal sealed class AuthenticationFlowService(
    IRequestOtpByEmailService requestOtpByEmailService,
    ILoginWithOtpService loginWithOtpService,
    IRefreshTokenExchangeService refreshTokenExchangeService)
    : IAuthenticationFlowService
{
    public Task<Result> RequestOtpByEmailAsync(string emailAddress, CancellationToken cancellationToken = default) =>
        requestOtpByEmailService.RequestOtpByEmailAsync(emailAddress, cancellationToken);

    public Task<Result<AuthTokens>> LoginWithOtpAsync(string email, string otp,
        CancellationToken cancellationToken = default) =>
        loginWithOtpService.LoginWithOtpAsync(email, otp, cancellationToken);

    public Task<Result<AuthTokens>> RefreshTokenAsync(string refreshTokenValue,
        CancellationToken cancellationToken = default) =>
        refreshTokenExchangeService.RefreshTokenAsync(refreshTokenValue, cancellationToken);
}