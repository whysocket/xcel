using Domain.Entities;
using Domain.Results;
using Microsoft.Extensions.Logging;
using Xcel.Services.Auth.Interfaces.Repositories;
using Xcel.Services.Auth.Interfaces.Services.Otp;

namespace Xcel.Services.Auth.Implementations.Services.Otp;

internal static class ValidateOtpServiceErrors
{
    internal static Error InvalidOrExpiredOtp() =>
        new(ErrorType.Unauthorized, "OTP expired or not found.");
}

internal sealed class ValidateOtpCommand(
    IOtpRepository otpRepository,
    ILogger<ValidateOtpCommand> logger) : IValidateOtpCommand
{
    private const string ServiceName = "[ValidateOtpCommand]";

    public async Task<Result> ValidateOtpAsync(Person person, string otpCode, CancellationToken cancellationToken = default)
    {
        logger.LogInformation($"{ServiceName} - Validating OTP for PersonId: {person.Id}");

        var otp = await otpRepository.GetOtpByPersonIdAsync(person.Id, cancellationToken);
        if (otp is null || otp.OtpCode != otpCode)
        {
            logger.LogWarning($"{ServiceName} - OTP not found or does not match for PersonId: {person.Id}");
            return Result.Fail(ValidateOtpServiceErrors.InvalidOrExpiredOtp());
        }

        await otpRepository.DeletePreviousOtpsByPersonIdAsync(person.Id, cancellationToken);
        await otpRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation($"{ServiceName} - OTP validated successfully for PersonId: {person.Id}");
        return Result.Ok();
    }
}