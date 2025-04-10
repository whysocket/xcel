using Domain.Entities;
using Domain.Interfaces.Repositories.Shared;

namespace Domain.Interfaces.Repositories;

public interface IPersonsRepository : IGenericRepository<Person>
{
    Task<Person?> GetByEmailAsync(string emailAddress, CancellationToken cancellationToken = default);

    Task<Person?> GetDeletedByIdAsync(Guid id, CancellationToken cancellationToken = default);
    
    Task<PageResult<Person>> GetAllDeletedAsync(
        PageRequest request,
        CancellationToken cancellationToken = default);
}