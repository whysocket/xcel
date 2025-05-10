using Domain.Entities;
using Domain.Interfaces.Repositories;
using Infra.Repositories.Shared;
using Microsoft.EntityFrameworkCore;

namespace Infra.Repositories;

internal class AvailabilityRulesRepository(AppDbContext dbContext)
    : GenericRepository<AvailabilityRule>(dbContext),
        IAvailabilityRulesRepository
{
    public async Task<List<AvailabilityRule>> GetByOwnerAsync(
        Guid ownerId,
        AvailabilityOwnerType ownerType,
        CancellationToken cancellationToken = default
    )
    {
        return await DbContext
            .Set<AvailabilityRule>()
            .Where(r => r.OwnerId == ownerId && r.OwnerType == ownerType)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<AvailabilityRule>> GetRulesActiveOnDateAsync(
        Guid ownerId,
        DateTime date,
        CancellationToken cancellationToken = default
    )
    {
        var compareDate = date.Date;

        // Note: This query finds ALL rules for the owner (regardless of RuleType) that are
        // active on the given date based on their DayOfWeek AND ActiveFromUtc/ActiveUntilUtc date range.
        var rules = await DbContext
            .Set<AvailabilityRule>()
            .Where(r =>
                r.OwnerId == ownerId
                && r.DayOfWeek == date.DayOfWeek // Rule's day of week must match the requested date's day
                && compareDate >= r.ActiveFromUtc.Date // The date must be within the rule's active date range
                && (r.ActiveUntilUtc == null || compareDate <= r.ActiveUntilUtc.Value.Date)
            )
            .ToListAsync(cancellationToken);
        
        return rules;
    }

    public async Task<List<AvailabilityRule>> GetByOwnerAndDateRangeAsync(
        Guid ownerId,
        AvailabilityOwnerType ownerType,
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default
    )
    {
        return await DbContext
            .Set<AvailabilityRule>()
            .Where(r =>
                r.OwnerId == ownerId
                && r.OwnerType == ownerType
                && (
                    (r.ActiveUntilUtc == null && r.ActiveFromUtc <= toDate.Date)
                    || (
                        r.ActiveUntilUtc != null
                        && r.ActiveFromUtc <= toDate.Date
                        && r.ActiveUntilUtc.Value.Date >= fromDate.Date
                    )
                )
            )
            .ToListAsync(cancellationToken);
    }

    public async Task DeleteNonExcludedAvailabilityRulesByOwnerAsync(
        Guid ownerId,
        AvailabilityOwnerType ownerType,
        CancellationToken cancellationToken = default
    )
    {
        var rulesToDelete = await DbContext
            .Set<AvailabilityRule>()
            .Where(r =>
                    r.OwnerId == ownerId
                    && r.OwnerType == ownerType
                    && r.RuleType == AvailabilityRuleType.AvailabilityStandard
            )
            .ToListAsync(cancellationToken);

        if (rulesToDelete.Any())
        {
            DbContext.RemoveRange(rulesToDelete);
        }
    }
}
