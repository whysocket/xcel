using Domain.Entities;
using Domain.Results;

namespace Xcel.Services.Auth.Interfaces.Services;

public interface IUserService
{
    Task<Result<Person>> CreateAccountAsync(Person person, CancellationToken cancellationToken = default);
    Task<Result> DeleteAccountAsync(Guid personId, CancellationToken cancellationToken = default);
}