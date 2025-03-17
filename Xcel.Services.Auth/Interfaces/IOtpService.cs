using Domain.Entities;

namespace Xcel.Services.Auth.Interfaces;

internal interface IOtpService
{
    Task<string> GenerateOtpAsync(Person person, CancellationToken cancellationToken = default);
}