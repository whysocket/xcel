using Domain.Entities;
using Domain.Results;

namespace Xcel.Services.Auth.Interfaces.Services.Otp;

internal interface IGenerateOtpCommand
{
    Task<Result<string>> GenerateOtpAsync(Person person, CancellationToken cancellationToken = default);
}