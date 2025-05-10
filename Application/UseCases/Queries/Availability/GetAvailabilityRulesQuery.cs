
namespace Application.UseCases.Queries.Availability;

/// <summary>
/// Returns all availability rules (including exclusions) for a given person.
/// </summary>
public interface IGetAvailabilityRulesQuery
{
    Task<Result<List<AvailabilityRuleDto>>> ExecuteAsync(
        Guid ownerId,
        AvailabilityOwnerType ownerType,
        CancellationToken cancellationToken = default
    );
}

public record AvailabilityRuleDto(
    Guid Id,
    DayOfWeek DayOfWeek,
    TimeSpan StartTimeUtc,
    TimeSpan EndTimeUtc,
    DateTime ActiveFromUtc,
    DateTime? ActiveUntilUtc,
    AvailabilityRuleType RuleType // Changed from bool IsExcluded
);

internal sealed class GetAvailabilityRulesQuery(
    IAvailabilityRulesRepository repository,
    ILogger<GetAvailabilityRulesQuery> logger
) : IGetAvailabilityRulesQuery
{
    private const string ServiceName = "[GetAvailabilityRulesQuery]";

    public async Task<Result<List<AvailabilityRuleDto>>> ExecuteAsync(
        Guid ownerId,
        AvailabilityOwnerType ownerType,
        CancellationToken cancellationToken = default
    )
    {
        logger.LogInformation(
            "{Service} Fetching availability rules for {OwnerType} {OwnerId}",
            ServiceName,
            ownerType,
            ownerId
        );

        var rules = await repository.GetByOwnerAsync(ownerId, ownerType, cancellationToken);

        var result = rules
            .Select(r => new AvailabilityRuleDto(
                r.Id,
                r.DayOfWeek,
                r.StartTimeUtc,
                r.EndTimeUtc,
                r.ActiveFromUtc.Date, 
                r.ActiveUntilUtc?.Date,
                r.RuleType
            ))
            .ToList();

        logger.LogInformation(
            "{Service} Found {Count} rules for {OwnerId}",
            ServiceName,
            result.Count,
            ownerId
        );

        return Result.Ok(result);
    }
}