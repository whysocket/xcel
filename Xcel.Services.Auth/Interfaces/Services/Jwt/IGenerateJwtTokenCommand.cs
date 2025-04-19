using Domain.Entities;
using Domain.Results;

namespace Xcel.Services.Auth.Interfaces.Services.Jwt;

internal interface IGenerateJwtTokenCommand
{
    Task<Result<string>> GenerateAsync(Person person, CancellationToken cancellationToken = default);
}