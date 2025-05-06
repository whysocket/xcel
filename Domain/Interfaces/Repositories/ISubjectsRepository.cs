using Domain.Entities;
using Domain.Interfaces.Repositories.Shared;

namespace Domain.Interfaces.Repositories;

public interface ISubjectsRepository : IGenericRepository<Subject>
{
    Task<bool> ExistsByName(string name, CancellationToken cancellationToken = default);

    Task<PageResult<Subject>> GetAllWithQualificationsAsync(
        PageRequest pageRequest,
        CancellationToken cancellationToken = default
    );
}
