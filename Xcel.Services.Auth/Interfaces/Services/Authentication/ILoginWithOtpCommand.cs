using Domain.Results;
using Xcel.Services.Auth.Models;

namespace Xcel.Services.Auth.Interfaces.Services.Authentication;

internal interface ILoginWithOtpCommand
{
    Task<Result<AuthTokens>> LoginWithOtpAsync(string email, string otp, CancellationToken cancellationToken = default);
}