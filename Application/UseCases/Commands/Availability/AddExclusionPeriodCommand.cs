namespace Application.UseCases.Commands.Availability;

public enum ExclusionType
{
    /// <summary>
    /// The exclusion applies to the entire day(s).
    /// </summary>
    FullDay,

    /// <summary>
    /// The exclusion applies only to a specific time range on the day(s).
    /// </summary>
    SpecificTime,
}

public record ExclusionPeriodInput(
    Guid OwnerId,
    AvailabilityOwnerType OwnerType,
    DateTime StartDateUtc, // Start date of the exclusion period (inclusive)
    DateTime EndDateUtc, // End date of the exclusion period (inclusive)
    ExclusionType Type,
    TimeSpan? StartTimeUtc = null, // Required if Type is SpecificTime
    TimeSpan? EndTimeUtc = null // Required if Type is SpecificTime
);

/// <summary>
/// Adds an exclusion period during which a person is not available.
/// Can add full-day or specific-time exclusions over a date range.
/// </summary>
public interface IAddExclusionPeriodCommand
{
    Task<Result> ExecuteAsync(
        ExclusionPeriodInput input,
        CancellationToken cancellationToken = default
    );
}

internal static class AddExclusionPeriodCommandErrors
{
    internal static Error PersonNotFound(Guid id) =>
        new(ErrorType.NotFound, $"The person with ID '{id}' does not exist.");

    internal static Error InvalidDateRange =>
        new(ErrorType.Validation, "Start date must be before or equal to end date.");

    internal static Error SpecificTimeRequired =>
        new(
            ErrorType.Validation,
            "Start time and End time are required for SpecificTime exclusion type."
        );

    internal static Error InvalidTimeRange =>
        new(
            ErrorType.Validation,
            "Start time must be before end time for SpecificTime exclusion type."
        );
}

internal sealed class AddExclusionPeriodCommand(
    IAvailabilityRulesRepository repository,
    IPersonsRepository personRepository,
    ILogger<AddExclusionPeriodCommand> logger
) : IAddExclusionPeriodCommand
{
    private const string ServiceName = "[AddExclusionPeriodCommand]";

    public async Task<Result> ExecuteAsync(
        ExclusionPeriodInput input,
        CancellationToken cancellationToken = default
    )
    {
        logger.LogInformation(
            "{Service} Attempting to add {Type} exclusion period for {OwnerType} {OwnerId} from {Start:yyyy-MM-dd} to {End:yyyy-MM-dd}",
            ServiceName,
            input.Type,
            input.OwnerType,
            input.OwnerId,
            input.StartDateUtc.Date,
            input.EndDateUtc.Date
        );

        if (input.StartDateUtc.Date > input.EndDateUtc.Date)
        {
            logger.LogWarning(
                "{Service} Invalid date range: Start {Start} > End {End}",
                ServiceName,
                input.StartDateUtc.Date,
                input.EndDateUtc.Date
            );
            return Result.Fail(AddExclusionPeriodCommandErrors.InvalidDateRange);
        }

        TimeSpan startTime;
        TimeSpan endTime;
        AvailabilityRuleType ruleType;

        if (input.Type == ExclusionType.SpecificTime)
        {
            if (input.StartTimeUtc is null || input.EndTimeUtc is null)
            {
                logger.LogWarning(
                    "{Service} SpecificTime exclusion requires StartTimeUtc and EndTimeUtc.",
                    ServiceName
                );
                return Result.Fail(AddExclusionPeriodCommandErrors.SpecificTimeRequired);
            }
            startTime = input.StartTimeUtc.Value;
            endTime = input.EndTimeUtc.Value;
            ruleType = AvailabilityRuleType.ExclusionTimeBased;

            if (startTime >= endTime)
            {
                logger.LogWarning(
                    "{Service} SpecificTime exclusion has invalid time range: {Start}-{End}",
                    ServiceName,
                    startTime,
                    endTime
                );
                return Result.Fail(AddExclusionPeriodCommandErrors.InvalidTimeRange);
            }
        }
        else // ExclusionType.FullDay
        {
            startTime = TimeSpan.Zero;
            endTime = TimeSpan.FromDays(1);
            ruleType = AvailabilityRuleType.ExclusionFullDay;
        }

        var person = await personRepository.GetByIdAsync(input.OwnerId, cancellationToken);
        if (person is null)
        {
            logger.LogWarning(
                "{Service} - Person not found: {OwnerId}",
                ServiceName,
                input.OwnerId
            );
            return Result.Fail(AddExclusionPeriodCommandErrors.PersonNotFound(input.OwnerId));
        }

        var rules = new List<AvailabilityRule>();

        // Create one rule per day in the date range
        for (
            var date = input.StartDateUtc.Date;
            date <= input.EndDateUtc.Date;
            date = date.AddDays(1)
        )
        {
            rules.Add(
                new AvailabilityRule
                {
                    Id = Guid.NewGuid(),
                    OwnerId = input.OwnerId,
                    Owner = person, // Assuming EF Core tracking or not needed here for AddRange
                    OwnerType = input.OwnerType,
                    RuleType = ruleType,
                    DayOfWeek = date.DayOfWeek, // Day of week for this specific date
                    StartTimeUtc = startTime,
                    EndTimeUtc = endTime,
                    ActiveFromUtc = date, // Rule applies only to this specific date
                    ActiveUntilUtc = date, // Rule applies only to this specific date
                }
            );
        }

        // This command adds exclusion rules. It does not automatically check for or remove
        // conflicting availability rules or booked interviews.

        await repository.AddRangeAsync(rules, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "{Service} {Type} exclusion period added from {Start:yyyy-MM-dd} to {End:yyyy-MM-dd} for {OwnerType} {OwnerId}. Created {Count} rules.",
            ServiceName,
            input.Type,
            input.StartDateUtc.Date,
            input.EndDateUtc.Date,
            input.OwnerType,
            input.OwnerId,
            rules.Count
        );

        // TODO: After adding exclusion rules, consider checking for existing booked interviews
        //       that now fall within these new exclusion periods. Notify the owner (and potentially
        //       the applicant) about the conflict so they can manually reschedule.

        return Result.Ok();
    }
}
