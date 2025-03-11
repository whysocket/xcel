using Domain.Entities;
using Domain.Interfaces.Repositories.Shared;

namespace Domain.Interfaces.Repositories;

public interface ITutorsRepository : IGenericRepository<Tutor>
{
    Task<Tutor?> GetTutorWithDocuments(Guid id, CancellationToken cancellationToken = default);
}
