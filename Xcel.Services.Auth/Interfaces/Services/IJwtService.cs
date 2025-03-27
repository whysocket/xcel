using Domain.Entities;
using Domain.Results;

namespace Xcel.Services.Auth.Interfaces.Services;

public interface IJwtService
{
    Task<Result<string>> GenerateAsync(Person person, CancellationToken cancellationToken = default);
}