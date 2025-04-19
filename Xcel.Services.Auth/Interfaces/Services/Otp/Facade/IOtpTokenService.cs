using Domain.Entities;
using Domain.Results;

namespace Xcel.Services.Auth.Interfaces.Services.Otp.Facade;

internal interface IOtpTokenService
{
    Task<Result<string>> GenerateAsync(Person person, CancellationToken cancellationToken = default);
    Task<Result> ValidateAsync(Person person, string otpCode, CancellationToken cancellationToken = default);
}