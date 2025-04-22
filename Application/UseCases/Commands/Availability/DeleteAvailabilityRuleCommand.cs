namespace Application.UseCases.Commands.Availability;

/// <summary>
/// Deletes a specific availability or exclusion rule by ID for a given person.
/// </summary>
public interface IDeleteAvailabilityRuleCommand
{
    Task<Result> ExecuteAsync(DeleteAvailabilityRuleInput input, CancellationToken cancellationToken = default);
}

public record DeleteAvailabilityRuleInput(Guid RuleId, Guid OwnerId, AvailabilityOwnerType OwnerType);

internal static class DeleteAvailabilityRuleCommandErrors
{
    internal static Error RuleNotFound(Guid ruleId) =>
        new(ErrorType.NotFound, $"Availability rule with ID '{ruleId}' was not found.");

    internal static Error Unauthorized(Guid ruleId) =>
        new(ErrorType.Forbidden, $"You are not authorized to delete rule '{ruleId}'.");
}

internal sealed class DeleteAvailabilityRuleCommand(
    IAvailabilityRulesRepository repository,
    ILogger<DeleteAvailabilityRuleCommand> logger
) : IDeleteAvailabilityRuleCommand
{
    private const string ServiceName = "[DeleteAvailabilityRuleCommand]";

    public async Task<Result> ExecuteAsync(DeleteAvailabilityRuleInput input, CancellationToken cancellationToken = default)
    {
        var rule = await repository.GetByIdAsync(input.RuleId, cancellationToken);
        if (rule is null)
        {
            logger.LogWarning("{Service} Rule not found: {RuleId}", ServiceName, input.RuleId);
            return Result.Fail(DeleteAvailabilityRuleCommandErrors.RuleNotFound(input.RuleId));
        }

        if (rule.OwnerId != input.OwnerId || rule.OwnerType != input.OwnerType)
        {
            logger.LogWarning("{Service} Unauthorized delete attempt for RuleId: {RuleId}", ServiceName, input.RuleId);
            return Result.Fail(DeleteAvailabilityRuleCommandErrors.Unauthorized(input.RuleId));
        }

        repository.Remove(rule);
        await repository.SaveChangesAsync(cancellationToken);

        logger.LogInformation("{Service} Deleted availability rule {RuleId} for {OwnerType} {OwnerId}",
            ServiceName, input.RuleId, input.OwnerType, input.OwnerId);

        return Result.Ok();
    }
}
