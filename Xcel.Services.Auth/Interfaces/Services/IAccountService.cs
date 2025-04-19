using Domain.Results;

namespace Xcel.Services.Auth.Interfaces.Services;

internal interface IAccountService
{
    Task<Result> RequestOtpByEmailAsync(
        string emailAddress,
        CancellationToken cancellationToken = default);
}