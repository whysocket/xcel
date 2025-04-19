using Domain.Results;
using Xcel.Services.Auth.Models;

namespace Xcel.Services.Auth.Features.Authentication.Commands.Interfaces;

internal interface ILoginWithOtpCommand
{
    Task<Result<AuthTokens>> ExecuteAsync(string email, string otp, CancellationToken cancellationToken = default);
}