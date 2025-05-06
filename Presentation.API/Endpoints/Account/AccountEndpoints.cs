using Microsoft.AspNetCore.Mvc;
using Presentation.API.Endpoints.Account.Requests;
using Presentation.API.Endpoints.Account.Responses;
using Xcel.Services.Auth.Public;

namespace Presentation.API.Endpoints.Account;

internal static class AccountEndpoints
{
    private const string DefaultTag = "Account";

    internal static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // Request OTP for Login
        endpoints
            .MapPost(
                Endpoints.Accounts.Login,
                async (IAuthServiceSdk authService, [FromBody] LoginRequest loginRequest) =>
                {
                    var result = await authService.RequestOtpByEmailAsync(loginRequest.Email);

                    return result.IsSuccess
                        ? Results.Ok(new { Message = "Check your email" })
                        : result.MapProblemDetails();
                }
            )
            .WithName("Account.RequestOtp")
            .WithTags(DefaultTag)
            .WithSummary("Request OTP for login.")
            .WithDescription("Sends an OTP to the user's email for login verification.");

        // Login with OTP
        endpoints
            .MapPost(
                Endpoints.Accounts.LoginWithOtp,
                async (
                    IAuthServiceSdk authService,
                    [FromBody] LoginWithOtpRequest loginWithOtpRequest,
                    CancellationToken cancellationToken
                ) =>
                {
                    var result = await authService.LoginWithOtpAsync(
                        loginWithOtpRequest.Email,
                        loginWithOtpRequest.OtpCode,
                        cancellationToken
                    );

                    return result.IsSuccess
                        ? Results.Ok(
                            new AuthTokensResponse(result.Value.JwtToken, result.Value.RefreshToken)
                        )
                        : result.MapProblemDetails();
                }
            )
            .WithName("Account.LoginWithOtp")
            .WithTags(DefaultTag)
            .WithSummary("Login with OTP.")
            .WithDescription("Logs in the user using the provided OTP code and email.");

        // Refresh Token
        endpoints
            .MapPost(
                Endpoints.Accounts.Refresh,
                async (
                    IAuthServiceSdk authService,
                    [FromBody] RefreshTokenRequest refreshTokenRequest,
                    CancellationToken cancellationToken
                ) =>
                {
                    var result = await authService.ExchangeRefreshTokenAsync(
                        refreshTokenRequest.RefreshToken,
                        cancellationToken
                    );

                    return result.IsSuccess
                        ? Results.Ok(
                            new AuthTokensResponse(result.Value.JwtToken, result.Value.RefreshToken)
                        )
                        : result.MapProblemDetails();
                }
            )
            .WithName("Account.RefreshToken")
            .WithTags(DefaultTag)
            .WithSummary("Refresh authentication token.")
            .WithDescription("Refreshes the user's authentication token using a refresh token.");

        // Delete Account
        endpoints
            .MapDelete(
                Endpoints.Accounts.Delete,
                async (Guid personId, IAuthServiceSdk authService) =>
                {
                    var result = await authService.DeleteAccountAsync(personId);

                    return result.IsSuccess ? Results.NoContent() : Results.NotFound(result.Errors);
                }
            )
            .WithName("Account.DeleteAccount")
            .WithTags(DefaultTag)
            .WithSummary("Delete user account.")
            .WithDescription("Deletes the user account associated with the given personId.");

        return endpoints;
    }
}
