using Domain.Entities;
using Domain.Results;

namespace Xcel.Services.Auth.Features.Otp.Commands.Interfaces;

internal interface IValidateOtpCommand
{
    Task<Result> ExecuteAsync(Person person, string otpCode, CancellationToken cancellationToken = default);
}