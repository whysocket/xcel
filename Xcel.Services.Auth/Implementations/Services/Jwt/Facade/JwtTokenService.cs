using Domain.Entities;
using Domain.Results;
using Xcel.Services.Auth.Interfaces.Services.Jwt;
using Xcel.Services.Auth.Interfaces.Services.Jwt.Facade;

namespace Xcel.Services.Auth.Implementations.Services.Jwt.Facade;

internal sealed class JwtTokenService(
    IGenerateJwtTokenService generateJwtTokenService
) : IJwtTokenService
{
    public Task<Result<string>> GenerateAsync(Person person, CancellationToken cancellationToken = default)
        => generateJwtTokenService.GenerateAsync(person, cancellationToken);
}