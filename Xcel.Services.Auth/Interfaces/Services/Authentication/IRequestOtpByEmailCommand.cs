using Domain.Results;

namespace Xcel.Services.Auth.Interfaces.Services.Authentication;

internal interface IRequestOtpByEmailCommand
{
    Task<Result> RequestOtpByEmailAsync(string emailAddress, CancellationToken cancellationToken = default);
}