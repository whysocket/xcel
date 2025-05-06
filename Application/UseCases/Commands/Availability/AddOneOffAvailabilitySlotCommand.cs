namespace Application.UseCases.Commands.Availability;

/// <summary>
/// Adds a one-off availability slot for a person (reviewer or tutor),
/// not tied to any recurring rule.
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
        new(ErrorType.Validation, "Slot overlaps with an existing availability.");
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
        if (input.StartUtc >= input.EndUtc)
        {
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
            Owner = person,
            OwnerType = input.OwnerType,
            DayOfWeek = input.StartUtc.DayOfWeek,
            StartTimeUtc = input.StartUtc.TimeOfDay,
            EndTimeUtc = input.EndUtc.TimeOfDay,
            ActiveFromUtc = input.StartUtc.Date,
            ActiveUntilUtc = input.StartUtc.Date,
            IsExcluded = false,
        };

        var existingRules = await repository.GetByOwnerAndDateAsync(
            input.OwnerId,
            input.StartUtc.Date,
            cancellationToken
        );
        if (
            existingRules.Any(x =>
                TimesOverlap(x.StartTimeUtc, x.EndTimeUtc, rule.StartTimeUtc, rule.EndTimeUtc)
            )
        )
        {
            return Result.Fail(AddOneOffAvailabilitySlotCommandErrors.OverlappingSlot);
        }

        await repository.AddAsync(rule, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "{Service} One-off slot added for {OwnerType} {OwnerId} on {Date}",
            ServiceName,
            input.OwnerType,
            input.OwnerId,
            input.StartUtc.Date
        );
        return Result.Ok();
    }

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
