namespace Application.UseCases.Commands.Availability;

/// <summary>
/// Defines recurring or one-off availability (or exclusion) rules for a person (tutor, reviewer, etc.).
/// </summary>
public interface ISetAvailabilityRulesCommand
{
    Task<Result> ExecuteAsync(Guid ownerId, AvailabilityOwnerType ownerType, List<AvailabilityRuleInput> rules,
        CancellationToken cancellationToken = default);
}

public record AvailabilityRuleInput(
    DayOfWeek DayOfWeek,
    TimeSpan StartTimeUtc,
    TimeSpan EndTimeUtc,
    DateTime ActiveFromUtc,
    DateTime? ActiveUntilUtc = null,
    bool IsExcluded = false);

internal static class SetAvailabilityRulesCommandErrors
{
    internal static Error PersonNotFound(Guid personId) =>
        new(ErrorType.NotFound, $"The person with ID '{personId}' does not exist.");
}

internal sealed class SetAvailabilityRulesCommand(
    IAvailabilityRulesRepository repository,
    IPersonsRepository personRepository,
    ILogger<SetAvailabilityRulesCommand> logger
) : ISetAvailabilityRulesCommand
{
    private const string ServiceName = "[SetAvailabilityRulesCommand]";

    public async Task<Result> ExecuteAsync(
        Guid ownerId,
        AvailabilityOwnerType ownerType,
        List<AvailabilityRuleInput> rules,
        CancellationToken cancellationToken = default)
    {
        var person = await personRepository.GetByIdAsync(ownerId, cancellationToken);
        if (person is null)
        {
            logger.LogWarning($"{ServiceName} - Person not found: {ownerId}");
            return Result.Fail(SetAvailabilityRulesCommandErrors.PersonNotFound(ownerId));
        }

        logger.LogInformation("{Service} Setting availability rules for {OwnerType} {OwnerId}", ServiceName, ownerType,
            ownerId);

        if (!rules.Any())
        {
            return Result.Fail(new Error(ErrorType.Validation, "At least one rule must be submitted."));
        }

        var domainRules = rules.Select(input => new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Owner = person,
            OwnerType = ownerType,
            DayOfWeek = input.DayOfWeek,
            StartTimeUtc = input.StartTimeUtc,
            EndTimeUtc = input.EndTimeUtc,
            ActiveFromUtc = input.ActiveFromUtc,
            ActiveUntilUtc = input.ActiveUntilUtc,
            IsExcluded = input.IsExcluded
        }).ToList();

        await repository.DeleteByOwnerAsync(ownerId, ownerType, cancellationToken);
        await repository.AddRangeAsync(domainRules, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        logger.LogInformation("{Service} Set {Count} rules for {OwnerType} {OwnerId}", ServiceName, domainRules.Count,
            ownerType, ownerId);
        return Result.Ok();
    }
}