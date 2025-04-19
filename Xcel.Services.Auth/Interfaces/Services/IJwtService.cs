using Domain.Entities;
using Domain.Results;

namespace Xcel.Services.Auth.Interfaces.Services;

internal interface IJwtService
{
    Task<Result<string>> GenerateAsync(Person person, CancellationToken cancellationToken = default);
}