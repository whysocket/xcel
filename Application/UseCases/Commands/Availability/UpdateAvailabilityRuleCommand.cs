namespace Application.UseCases.Commands.Availability;

/// <summary>
/// Updates an existing availability or exclusion rule for a person (reviewer or tutor).
/// </summary>
public interface IUpdateAvailabilityRuleCommand
{
    Task<Result> ExecuteAsync(
        UpdateAvailabilityRuleInput input,
        CancellationToken cancellationToken = default
    );
}

public record UpdateAvailabilityRuleInput(
    Guid RuleId,
    Guid OwnerId,
    AvailabilityOwnerType OwnerType,
    TimeSpan StartTimeUtc,
    TimeSpan EndTimeUtc,
    DateTime ActiveFromUtc,
    DateTime? ActiveUntilUtc,
    bool IsExcluded
);

internal static class UpdateAvailabilityRuleCommandErrors
{
    internal static Error RuleNotFound(Guid id) =>
        new(ErrorType.NotFound, $"Availability rule with ID '{id}' was not found.");

    internal static Error Unauthorized(Guid id) =>
        new(ErrorType.Forbidden, $"You are not authorized to update rule '{id}'.");

    internal static Error InvalidTimeRange =>
        new(ErrorType.Validation, "Start time must be before end time.");
}

internal sealed class UpdateAvailabilityRuleCommand(
    IAvailabilityRulesRepository repository,
    ILogger<UpdateAvailabilityRuleCommand> logger
) : IUpdateAvailabilityRuleCommand
{
    private const string ServiceName = "[UpdateAvailabilityRuleCommand]";

    public async Task<Result> ExecuteAsync(
        UpdateAvailabilityRuleInput input,
        CancellationToken cancellationToken = default
    )
    {
        var rule = await repository.GetByIdAsync(input.RuleId, cancellationToken);
        if (rule is null)
        {
            logger.LogWarning("{Service} Rule not found: {RuleId}", ServiceName, input.RuleId);
            return Result.Fail(UpdateAvailabilityRuleCommandErrors.RuleNotFound(input.RuleId));
        }

        if (rule.OwnerId != input.OwnerId || rule.OwnerType != input.OwnerType)
        {
            logger.LogWarning(
                "{Service} Unauthorized update attempt for RuleId: {RuleId}",
                ServiceName,
                input.RuleId
            );
            return Result.Fail(UpdateAvailabilityRuleCommandErrors.Unauthorized(input.RuleId));
        }

        if (!input.IsExcluded && input.StartTimeUtc >= input.EndTimeUtc)
        {
            return Result.Fail(UpdateAvailabilityRuleCommandErrors.InvalidTimeRange);
        }

        rule.StartTimeUtc = input.StartTimeUtc;
        rule.EndTimeUtc = input.EndTimeUtc;
        rule.ActiveFromUtc = input.ActiveFromUtc;
        rule.ActiveUntilUtc = input.ActiveUntilUtc;
        rule.IsExcluded = input.IsExcluded;
        rule.DayOfWeek = input.ActiveFromUtc.DayOfWeek;

        repository.Update(rule);
        await repository.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "{Service} Updated rule {RuleId} for {OwnerType} {OwnerId}",
            ServiceName,
            input.RuleId,
            input.OwnerType,
            input.OwnerId
        );
        return Result.Ok();
    }
}
