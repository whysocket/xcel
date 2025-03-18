using Domain.Entities;
using Domain.Results;

namespace Xcel.Services.Auth.Interfaces;

public interface IAccountService
{
    Task<Result<Person>> CreateAccountAsync(Person person, CancellationToken cancellationToken = default);
    Task<Result> DeleteAccountAsync(Guid personId, CancellationToken cancellationToken = default);
}
