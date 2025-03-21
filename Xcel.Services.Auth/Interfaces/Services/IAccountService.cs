using Domain.Entities;
using Domain.Results;

namespace Xcel.Services.Auth.Interfaces.Services;

public interface IAccountService
{
    Task<Result<Person>> CreateAccountAsync(
        Person person,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteAccountAsync(
        Guid personId,
        CancellationToken cancellationToken = default);

    Task<Result<string>> LoginWithOtpAsync(
        string email,
        string otp,
        CancellationToken cancellationToken = default);

    Task<Result> RequestOtpByEmailAsync(
        string emailAddress,
        CancellationToken cancellationToken = default);
}