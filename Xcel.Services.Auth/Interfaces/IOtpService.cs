using Domain.Entities;
using Domain.Results;

namespace Xcel.Services.Auth.Interfaces;

internal interface IOtpService
{
    Task<Result<string>> GenerateOtpAsync(Person person, CancellationToken cancellationToken = default);
    
    Task<Result> ValidateOtpAsync(
        Person person, 
        string otpCode,
        CancellationToken cancellationToken = default);
}