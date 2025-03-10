using Domain.Entities;

namespace Domain.Interfaces.Services;

public interface IAccountService
{
    Task<Person> CreateAccountAsync(Person person, CancellationToken cancellationToken = default);
    Task<bool> CheckAccountExistanceByEmailAsync(string emailAddress, CancellationToken cancellationToken = default);
}
