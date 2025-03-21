using Presentation.API.Endpoints.Account;
using Xcel.Services.Auth.Interfaces.Services;

namespace Presentation.API.Endpoints;

public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder endpoints)
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

        return endpoints;
    }
}