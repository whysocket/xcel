namespace Application.UseCases.Commands.Availability;

/// <summary>
/// Adds an exclusion period (one or more dates) during which a person is not available,
/// overriding any existing recurring or one-off availability.
/// </summary>
public interface IAddExclusionPeriodCommand
{
    Task<Result> ExecuteAsync(
        ExclusionPeriodInput input,
        CancellationToken cancellationToken = default
    );
}

public record ExclusionPeriodInput(
    Guid OwnerId,
    AvailabilityOwnerType OwnerType,
    DateTime StartDateUtc,
    DateTime EndDateUtc
);

internal static class AddExclusionPeriodCommandErrors
{
    internal static Error PersonNotFound(Guid id) =>
        new(ErrorType.NotFound, $"The person with ID '{id}' does not exist.");

    internal static Error InvalidDateRange =>
        new(ErrorType.Validation, "Start date must be before or equal to end date.");
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
        if (input.StartDateUtc.Date > input.EndDateUtc.Date)
        {
            return Result.Fail(AddExclusionPeriodCommandErrors.InvalidDateRange);
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
                    Owner = person,
                    OwnerType = input.OwnerType,
                    DayOfWeek = date.DayOfWeek,
                    StartTimeUtc = TimeSpan.Zero,
                    EndTimeUtc = TimeSpan.Zero,
                    ActiveFromUtc = date,
                    ActiveUntilUtc = date,
                    IsExcluded = true,
                }
            );
        }

        await repository.AddRangeAsync(rules, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "{Service} Exclusion period added from {Start} to {End} for {OwnerType} {OwnerId}",
            ServiceName,
            input.StartDateUtc.Date,
            input.EndDateUtc.Date,
            input.OwnerType,
            input.OwnerId
        );

        return Result.Ok();
    }
}
