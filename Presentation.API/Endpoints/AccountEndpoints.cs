using Presentation.API.Endpoints.Account;
using Xcel.Services.Auth.Interfaces.Services;

namespace Presentation.API.Endpoints;

internal static class AccountEndpoints
{
    internal static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var accountGroup = endpoints.MapGroup("/account");

        accountGroup.MapPost("/login", async (
                IAccountService accountService,
                LoginRequest loginRequest) =>
            {
                var result = await accountService.RequestOtpByEmailAsync(loginRequest.Email);

                return result.IsSuccess
                    ? Results.Ok(new
                    {
                        Message = "Check your email"
                    })
                    : result.MapProblemDetails();
            })
            .WithName("RequestOtpByEmail")
            .WithTags("Account");

        accountGroup.MapPost("/login/otp", async (
                IAccountService accountService,
                LoginWithOtpRequest loginWithOtpRequest) =>
            {
                var result = await accountService.LoginWithOtpAsync(
                    loginWithOtpRequest.Email,
                    loginWithOtpRequest.OtpCode);

                return result.IsSuccess
                    ? Results.Ok(new
                    {
                        AccessToken = result.Value
                    })
                    : result.MapProblemDetails();
            })
            .WithName("LoginWithOtp")
            .WithTags("Account");

        accountGroup.MapDelete("/{personId}", async (Guid personId, IAccountService accountService) =>
            {
                var result = await accountService.DeleteAccountAsync(personId);
                return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Errors);
            })
            .WithName("DeleteAccount")
            .WithTags("Admin", "Accounts");

        return endpoints;
    }
}