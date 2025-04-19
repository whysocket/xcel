using Domain.Entities;
using Domain.Results;

namespace Xcel.Services.Auth.Interfaces.Services.Jwt.Facade;

internal interface IJwtTokenService
{
    Task<Result<string>> GenerateAsync(Person person, CancellationToken cancellationToken = default);
}