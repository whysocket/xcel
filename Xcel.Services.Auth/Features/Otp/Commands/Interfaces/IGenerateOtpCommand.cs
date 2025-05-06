using Domain.Entities;
using Domain.Results;

namespace Xcel.Services.Auth.Features.Otp.Commands.Interfaces;

internal interface IGenerateOtpCommand
{
    Task<Result<string>> ExecuteAsync(Person person, CancellationToken cancellationToken = default);
}
