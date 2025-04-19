using Domain.Entities;
using Domain.Results;

namespace Xcel.Services.Auth.Interfaces.Services.Otp;

internal interface IValidateOtpCommand
{
    Task<Result> ValidateOtpAsync(Person person, string otpCode, CancellationToken cancellationToken = default);
}