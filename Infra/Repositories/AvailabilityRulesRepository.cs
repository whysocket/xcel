using Domain.Entities;
using Domain.Interfaces.Repositories;
using Infra.Repositories.Shared;
using Microsoft.EntityFrameworkCore;

namespace Infra.Repositories;

internal class AvailabilityRulesRepository(AppDbContext dbContext)
    : GenericRepository<AvailabilityRule>(dbContext), IAvailabilityRulesRepository
{
    public async Task<List<AvailabilityRule>> GetByOwnerAsync(Guid ownerId, AvailabilityOwnerType ownerType, CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<AvailabilityRule>()
            .Where(r => r.OwnerId == ownerId && r.OwnerType == ownerType)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<AvailabilityRule>> GetByOwnerAndDateAsync(Guid ownerId, DateTime date, CancellationToken cancellationToken = default)
    {
        var dayOfWeek = date.DayOfWeek;
        return await DbContext.Set<AvailabilityRule>()
            .Where(r =>
                r.OwnerId == ownerId &&
                r.DayOfWeek == dayOfWeek &&
                date.Date >= r.ActiveFromUtc.Date &&
                (r.ActiveUntilUtc == null || date.Date <= r.ActiveUntilUtc.Value.Date))
            .ToListAsync(cancellationToken);
    }

    public async Task<List<AvailabilityRule>> GetByOwnerAndDateRangeAsync(
        Guid ownerId,
        AvailabilityOwnerType ownerType,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<AvailabilityRule>()
            .Where(r =>
                r.OwnerId == ownerId &&
                r.OwnerType == ownerType &&
                (
                    (r.ActiveUntilUtc == null && r.ActiveFromUtc <= toDate.Date) ||
                    (r.ActiveUntilUtc != null && r.ActiveFromUtc <= toDate.Date && r.ActiveUntilUtc.Value.Date >= fromDate.Date)
                ))
            .ToListAsync(cancellationToken);
    }

    public async Task DeleteByOwnerAsync(Guid ownerId, AvailabilityOwnerType ownerType, CancellationToken cancellationToken = default)
    {
        var rules = await DbContext.Set<AvailabilityRule>()
            .Where(r => r.OwnerId == ownerId && r.OwnerType == ownerType)
            .ToListAsync(cancellationToken);

        DbContext.RemoveRange(rules);
    }
}