using Domain.Results;

namespace Xcel.Services.Auth.Features.Authentication.Commands.Interfaces;

internal interface IRequestOtpByEmailCommand
{
    Task<Result> ExecuteAsync(string emailAddress, CancellationToken cancellationToken = default);
}
