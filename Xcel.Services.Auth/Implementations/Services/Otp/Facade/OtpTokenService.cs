using Domain.Entities;
using Domain.Results;
using Xcel.Services.Auth.Interfaces.Services.Otp;
using Xcel.Services.Auth.Interfaces.Services.Otp.Facade;

namespace Xcel.Services.Auth.Implementations.Services.Otp.Facade;

internal sealed class OtpTokenService(
    IGenerateOtpService generateOtpService,
    IValidateOtpService validateOtpService)
    : IOtpTokenService
{
    public Task<Result<string>> GenerateAsync(Person person, CancellationToken cancellationToken = default) =>
        generateOtpService.GenerateOtpAsync(person, cancellationToken);

    public Task<Result> ValidateAsync(Person person, string otpCode, CancellationToken cancellationToken = default) =>
        validateOtpService.ValidateOtpAsync(person, otpCode, cancellationToken);
}