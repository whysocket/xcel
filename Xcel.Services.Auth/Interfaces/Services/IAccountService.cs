using Domain.Entities;
using Domain.Results;

namespace Xcel.Services.Auth.Interfaces.Services;

public interface IAccountService
{
    Task<Result> RequestOtpByEmailAsync(
        string emailAddress,
        CancellationToken cancellationToken = default);
}