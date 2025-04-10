using Microsoft.AspNetCore.Mvc;
using Presentation.API.Endpoints.Account.Requests;
using Presentation.API.Endpoints.Account.Responses;
using Xcel.Services.Auth.Interfaces.Services;

namespace Presentation.API.Endpoints.Account;

internal static class AccountEndpoints
{
    internal static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var accountGroup = endpoints.MapGroup("/account");

        // Request OTP for Login
        accountGroup.MapPost("/login", async (
                IAccountService accountService,
                [FromBody] LoginRequest loginRequest) => 
            {
                var result = await accountService.RequestOtpByEmailAsync(loginRequest.Email);

                return result.IsSuccess
                    ? Results.Ok(new { Message = "Check your email" })
                    : result.MapProblemDetails();
            })
            .WithName("Account.RequestOtp") 
            .WithTags("Accounts");

        // Login with OTP
        accountGroup.MapPost("/login/otp", async (
                IAuthenticationService authenticationService,
                [FromBody] LoginWithOtpRequest loginWithOtpRequest, 
                CancellationToken cancellationToken) =>
            {
                var result = await authenticationService.LoginWithOtpAsync(
                    loginWithOtpRequest.Email,
                    loginWithOtpRequest.OtpCode,
                    cancellationToken);

                return result.IsSuccess
                    ? Results.Ok(new AuthTokensResponse(
                        result.Value.JwtToken,
                        result.Value.RefreshToken))
                    : result.MapProblemDetails(); 
            })
            .WithName("Account.Login") 
            .WithTags("Accounts");

        // Refresh Token
        accountGroup.MapPost("/refresh", async (
                IAuthenticationService authenticationService,
                [FromBody] RefreshTokenRequest refreshTokenRequest,
                CancellationToken cancellationToken) =>
            {
                var result = await authenticationService.RefreshTokenAsync(
                    refreshTokenRequest.RefreshToken,
                    cancellationToken);

                return result.IsSuccess
                    ? Results.Ok(new AuthTokensResponse(
                        result.Value.JwtToken,
                        result.Value.RefreshToken))
                    : result.MapProblemDetails();
            })
            .WithName("Account.Refresh") 
            .WithTags("Accounts");

        // Delete Account
        accountGroup.MapDelete("/{personId}", async (
                Guid personId,
                IUserService userService) =>
            {
                var result = await userService.DeleteAccountAsync(personId);
                return result.IsSuccess
                    ? Results.NoContent()
                    : Results.NotFound(result.Errors);
            })
            .WithName("Account.Delete") 
            .WithTags("Accounts");

        return endpoints;
    }
}



