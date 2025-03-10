using Domain.Entities;
using Domain.Interfaces.Repositories.Shared;

namespace Domain.Interfaces.Repositories;

public interface IPersonsRepository : IGenericRepository<Person>
{
    Task<Person?> FindByEmailAsync(string emailAddress, CancellationToken cancellationToken = default);
}