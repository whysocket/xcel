using Domain.Entities;
using Domain.Results;

namespace Xcel.Services.Auth.Features.Jwt.Commands.Interfaces;

internal interface IGenerateJwtTokenCommand
{
    Task<Result<string>> ExecuteAsync(Person person, CancellationToken cancellationToken = default);
}