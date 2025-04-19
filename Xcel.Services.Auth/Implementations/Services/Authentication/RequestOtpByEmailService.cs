using Domain.Interfaces.Repositories;
using Domain.Results;
using Microsoft.Extensions.Logging;
using Xcel.Services.Auth.Interfaces.Services.Authentication;
using Xcel.Services.Auth.Interfaces.Services.Otp.Facade;

namespace Xcel.Services.Auth.Implementations.Services.Authentication;

internal static class RequestOtpByEmailServiceErrors
{
    internal static Error PersonNotFound(string email) =>
        new(ErrorType.Unauthorized, $"The person with email address '{email}' was not found.");
}

internal sealed class RequestOtpByEmailService(
    IPersonsRepository personRepository,
    IOtpTokenService otpTokenService,
    ILogger<RequestOtpByEmailService> logger)
    : IRequestOtpByEmailService
{
    private const string ServiceName = "[RequestOtpByEmailService]";

    public async Task<Result> RequestOtpByEmailAsync(string emailAddress, CancellationToken cancellationToken = default)
    {
        logger.LogInformation($"{ServiceName} - Looking up person by email: {emailAddress}");

        var existingPerson = await personRepository.GetByEmailAsync(emailAddress, cancellationToken);
        if (existingPerson is null)
        {
            logger.LogWarning($"{ServiceName} - Person not found: {emailAddress}");
            return Result.Fail(RequestOtpByEmailServiceErrors.PersonNotFound(emailAddress));
        }

        var result = await otpTokenService.GenerateAsync(existingPerson, cancellationToken);

        if (result.IsFailure)
        {
            logger.LogWarning($"{ServiceName} - OTP generation failed: {emailAddress}");
            return Result.Fail(result.Errors);
        }

        logger.LogInformation($"{ServiceName} - OTP successfully sent to: {emailAddress}");
        return Result.Ok();
    }
}