namespace Application.UseCases.Commands.Availability;

/// <summary>
/// Adds a one-off availability slot for a person (reviewer or tutor).
/// Creates an AvailabilityRule with RuleType = AvailabilityRuleType.AvailabilityOneOff.
/// Validates against all existing rules active on that date.
/// </summary>
public interface IAddOneOffAvailabilitySlotCommand
{
    Task<Result> ExecuteAsync(
        OneOffAvailabilityInput input,
        CancellationToken cancellationToken = default
    );
}

public record OneOffAvailabilityInput(
    Guid OwnerId,
    AvailabilityOwnerType OwnerType,
    DateTime StartUtc,
    DateTime EndUtc
);

internal static class AddOneOffAvailabilitySlotCommandErrors
{
    internal static Error PersonNotFound(Guid personId) =>
        new(ErrorType.NotFound, $"The person with ID '{personId}' does not exist.");

    internal static Error InvalidTimeRange =>
        new(ErrorType.Validation, "Start time must be before end time.");

    internal static Error OverlappingSlot =>
        new(ErrorType.Validation, "Slot overlaps with an existing availability or exclusion.");
}

internal sealed class AddOneOffAvailabilitySlotCommand(
    IAvailabilityRulesRepository repository,
    IPersonsRepository personRepository,
    ILogger<AddOneOffAvailabilitySlotCommand> logger
) : IAddOneOffAvailabilitySlotCommand
{
    private const string ServiceName = "[AddOneOffAvailabilitySlotCommand]";

    public async Task<Result> ExecuteAsync(
        OneOffAvailabilityInput input,
        CancellationToken cancellationToken = default
    )
    {
        logger.LogInformation(
            "{Service} Attempting to add one-off slot for {OwnerType} {OwnerId} at {Start:yyyy-MM-dd HH:mm}",
            ServiceName,
            input.OwnerType,
            input.OwnerId,
            input.StartUtc
        );

        if (input.StartUtc >= input.EndUtc)
        {
            logger.LogWarning("{Service} Invalid time range: Start {Start} >= End {End}", ServiceName, input.StartUtc, input.EndUtc);
            return Result.Fail(AddOneOffAvailabilitySlotCommandErrors.InvalidTimeRange);
        }

        var person = await personRepository.GetByIdAsync(input.OwnerId, cancellationToken);
        if (person is null)
        {
            logger.LogWarning(
                "{Service} - Person not found: {PersonId}",
                ServiceName,
                input.OwnerId
            );
            return Result.Fail(
                AddOneOffAvailabilitySlotCommandErrors.PersonNotFound(input.OwnerId)
            );
        }

        var rule = new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            OwnerId = input.OwnerId,
            Owner = person, // Assuming EF Core tracking or not needed here
            OwnerType = input.OwnerType,
            RuleType = AvailabilityRuleType.AvailabilityOneOff, // Set the specific one-off type
            DayOfWeek = input.StartUtc.DayOfWeek, // Day of week for this specific date
            StartTimeUtc = input.StartUtc.TimeOfDay,
            EndTimeUtc = input.EndUtc.TimeOfDay,
            ActiveFromUtc = input.StartUtc.Date, // Active only on this date
            ActiveUntilUtc = input.StartUtc.Date, // Active only on this date
        };

        // Check for overlaps with ANY existing rule active on this date (Availability or Exclusion)
        // GetRulesActiveOnDateAsync fetches all rule types active on the date.
        var existingRules = await repository.GetRulesActiveOnDateAsync( // Renamed call
            input.OwnerId,
            input.StartUtc.Date,
            cancellationToken
        );

        // The TimesOverlap helper works on TimeSpan, so it checks time collision regardless of rule type,
        // provided GetRulesActiveOnDateAsync returns all active rules for the day.
        if (
            existingRules.Any(x =>
                TimesOverlap(x.StartTimeUtc, x.EndTimeUtc, rule.StartTimeUtc, rule.EndTimeUtc)
            )
        )
        {
            logger.LogWarning(
                 "{Service} One-off slot {Start}-{End} for {OwnerType} {OwnerId} on {Date:yyyy-MM-dd} overlaps with existing rule.",
                 ServiceName,
                 rule.StartTimeUtc,
                 rule.EndTimeUtc,
                 input.OwnerType,
                 input.OwnerId,
                 input.StartUtc.Date
             );
            return Result.Fail(AddOneOffAvailabilitySlotCommandErrors.OverlappingSlot);
        }

        await repository.AddAsync(rule, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "{Service} One-off slot added for {OwnerType} {OwnerId} on {Date:yyyy-MM-dd} from {Start} to {End}",
            ServiceName,
            input.OwnerType,
            input.OwnerId,
            input.StartUtc.Date,
            rule.StartTimeUtc,
            rule.EndTimeUtc
        );
        return Result.Ok();
    }

    /// <summary>
    /// Checks if two time ranges (TimeSpan) overlap. Assumes EndTime is exclusive.
    /// </summary>
    private static bool TimesOverlap(
        TimeSpan existingStart,
        TimeSpan existingEnd,
        TimeSpan newStart,
        TimeSpan newEnd
    )
    {
        return newStart < existingEnd && existingStart < newEnd;
    }
}