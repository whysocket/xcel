using Domain.Entities;
using Domain.Interfaces.Repositories.Shared;

namespace Domain.Interfaces.Repositories;

public interface IAvailabilityRulesRepository : IGenericRepository<AvailabilityRule>
{
    Task DeleteByOwnerAsync(
        Guid ownerId,
        AvailabilityOwnerType ownerType,
        CancellationToken cancellationToken = default
    );

    Task<List<AvailabilityRule>> GetByOwnerAndDateAsync(
        Guid ownerId,
        DateTime date,
        CancellationToken cancellationToken = default
    );

    Task<List<AvailabilityRule>> GetByOwnerAndDateRangeAsync(
        Guid ownerId,
        AvailabilityOwnerType ownerType,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default
    );

    Task<List<AvailabilityRule>> GetByOwnerAsync(
        Guid ownerId,
        AvailabilityOwnerType ownerType,
        CancellationToken cancellationToken = default
    );
}
