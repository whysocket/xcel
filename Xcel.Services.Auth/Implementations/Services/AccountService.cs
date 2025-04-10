using Domain.Interfaces.Repositories;
using Domain.Results;
using Xcel.Services.Auth.Interfaces.Services;

namespace Xcel.Services.Auth.Implementations.Services;

internal sealed class AccountService(
    IPersonsRepository personRepository,
    IOtpService otpService) : IAccountService
{
    public async Task<Result> RequestOtpByEmailAsync(string emailAddress, CancellationToken cancellationToken = default)
    {
        var existingPerson = await personRepository.GetByEmailAsync(emailAddress, cancellationToken);
        if (existingPerson is null)
        {
            return Result.Fail(new Error(
                ErrorType.Unauthorized,
                $"The person with email address '{emailAddress}' was not found."));
        }

        var otpResult = await otpService.GenerateOtpAsync(existingPerson, cancellationToken);
        if (otpResult.IsFailure)
        {
            return Result.Fail(otpResult.Errors);
        }

        return Result.Ok();
    }
}