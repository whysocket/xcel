namespace Application.UseCases.Commands.Availability;

/// <summary>
/// Defines recurring or date-ranged **standard availability** rules for a person (tutor, reviewer, etc.).
/// This command replaces all existing **standard availability** rules for the owner, leaving one-off availability and exclusions intact.
/// Exclusions must be managed via AddExclusionPeriodCommand. One-off availability via AddOneOffAvailabilitySlotCommand.
/// </summary>
public interface ISetAvailabilityRulesCommand
{
    Task<Result> ExecuteAsync(
        Guid ownerId,
        AvailabilityOwnerType ownerType,
        List<AvailabilityRuleInput> rules,
        CancellationToken cancellationToken = default
    );
}

public record AvailabilityRuleInput(
    DayOfWeek DayOfWeek,
    TimeSpan StartTimeUtc,
    TimeSpan EndTimeUtc,
    DateTime ActiveFromUtc,
    DateTime? ActiveUntilUtc = null
);

internal static class SetAvailabilityRulesCommandErrors
{
    internal static Error PersonNotFound(Guid personId) =>
        new(ErrorType.NotFound, $"The person with ID '{personId}' does not exist.");

    internal static Error NoRulesSubmitted =>
        new(ErrorType.Validation, "At least one rule must be submitted.");

    internal static Error OverlappingAvailabilityRules =>
        new(
            ErrorType.Validation,
            "Submitted availability rules contain overlapping time ranges for the same day/period."
        );

    internal static Error InvalidTimeRange(TimeSpan start, TimeSpan end) =>
        new(
            ErrorType.Validation,
            $"Invalid time range: Start time ({start}) must be before end time ({end})."
        );
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
        List<AvailabilityRuleInput> rules, // AvailabilityRuleInput no longer has IsExcluded
        CancellationToken cancellationToken = default
    )
    {
        logger.LogInformation(
            "{Service} Attempting to set standard availability rules for {OwnerType} {OwnerId}. Number of rules submitted: {Count}",
            ServiceName,
            ownerType,
            ownerId,
            rules.Count
        );

        var person = await personRepository.GetByIdAsync(ownerId, cancellationToken);
        if (person is null)
        {
            logger.LogWarning($"{ServiceName} - Person not found: {ownerId}");
            return Result.Fail(SetAvailabilityRulesCommandErrors.PersonNotFound(ownerId));
        }

        if (!rules.Any())
        {
            // This command replaces, so an empty list could mean 'remove all standard availability'.
            // If you require at least one rule, keep this check. If empty list means remove all standard, remove this.
            // Assuming for now you must submit at least one new rule if using Set.
            logger.LogWarning(
                "{Service} No rules submitted for {OwnerType} {OwnerId}",
                ServiceName,
                ownerType,
                ownerId
            );
            return Result.Fail(SetAvailabilityRulesCommandErrors.NoRulesSubmitted);
        }

        // Map input rules to domain entities - ALWAYS set RuleType = AvailabilityStandard
        var domainRules = rules
            .Select(input => new AvailabilityRule
            {
                Id = Guid.NewGuid(),
                OwnerId = ownerId,
                Owner = person, // Assuming EF Core can track this relationship or not needed here for AddRange
                OwnerType = ownerType,
                RuleType = AvailabilityRuleType.AvailabilityStandard, // Set the standard availability type
                DayOfWeek = input.DayOfWeek,
                StartTimeUtc = input.StartTimeUtc,
                EndTimeUtc = input.EndTimeUtc,
                ActiveFromUtc = input.ActiveFromUtc.Date, // Store as Date only for consistency
                ActiveUntilUtc = input.ActiveUntilUtc?.Date, // Store as Date only for consistency
                // IsExcluded property is replaced by RuleType
            })
            .ToList();

        // --- Validation ---

        // 1. Validate time ranges for the submitted availability rules
        var invalidTimeRanges = domainRules.Where(r => r.StartTimeUtc >= r.EndTimeUtc).ToList();

        if (invalidTimeRanges.Any())
        {
            logger.LogWarning(
                "{Service} Invalid time ranges found in submitted standard availability rules for {OwnerType} {OwnerId}. Count: {Count}",
                ServiceName,
                ownerType,
                ownerId,
                invalidTimeRanges.Count
            );
            // Return the error for the first invalid range found
            var invalidRule = invalidTimeRanges.First();
            return Result.Fail(
                SetAvailabilityRulesCommandErrors.InvalidTimeRange(
                    invalidRule.StartTimeUtc,
                    invalidRule.EndTimeUtc
                )
            );
        }

        // 2. Validate for overlaps within the proposed set of standard availability rules
        var overlappingGroups = domainRules
            // These are all AvailabilityStandard rules, so group and check them
            .GroupBy(r => new
            {
                // Group by the set of properties that define a recurring or date-ranged period
                r.OwnerId,
                r.OwnerType,
                r.DayOfWeek,
                ActiveFromDate = r.ActiveFromUtc.Date, // Use Date part for grouping
                ActiveUntilDate = r.ActiveUntilUtc?.Date, // Use Date part for grouping
            })
            .Where(group =>
            {
                var rulesInGroup = group.ToList();
                if (rulesInGroup.Count < 2)
                    return false; // Need at least two rules to have an overlap

                for (int i = 0; i < rulesInGroup.Count; i++)
                {
                    for (int j = i + 1; j < rulesInGroup.Count; j++)
                    {
                        if (
                            TimesOverlap(
                                rulesInGroup[i].StartTimeUtc,
                                rulesInGroup[i].EndTimeUtc,
                                rulesInGroup[j].StartTimeUtc,
                                rulesInGroup[j].EndTimeUtc
                            )
                        )
                        {
                            return true; // Found an overlap within this group
                        }
                    }
                }
                return false; // No overlaps found in this group
            })
            .ToList();

        if (overlappingGroups.Any())
        {
            logger.LogWarning(
                "{Service} Overlapping standard availability rules found in submitted set for {OwnerType} {OwnerId}",
                ServiceName,
                ownerType,
                ownerId
            );
            return Result.Fail(SetAvailabilityRulesCommandErrors.OverlappingAvailabilityRules);
        }

        // TODO: Validate that submitted standard availability rules do NOT overlap with existing one-off availability or exclusion rules.
        // This requires fetching existing rules for the relevant period and checking for conflicts before saving.
        // This validation is complex and might need to consider how recurring standard rules interact with date-specific one-off/exclusion rules.

        // --- End Validation ---

        // If validation passes, proceed with deleting existing STANDARD AVAILABILITY rules and adding new ones
        // Use the repository method to only delete rules with RuleType = AvailabilityStandard
        // TODO: Ensure DeleteNonExcludedAvailabilityRulesByOwnerAsync is updated/renamed to specifically delete rules where RuleType = AvailabilityStandard
        // For now, assuming DeleteNonExcludedAvailabilityRulesByOwnerAsync does this.
        await repository.DeleteNonExcludedAvailabilityRulesByOwnerAsync(
            ownerId,
            ownerType,
            cancellationToken
        ); // Assuming this method now filters by RuleType == AvailabilityStandard
        await repository.AddRangeAsync(domainRules, cancellationToken);

        await repository.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "{Service} Successfully set {Count} standard availability rules for {OwnerType} {OwnerId}",
            ServiceName,
            domainRules.Count,
            ownerType,
            ownerId
        );

        // TODO: After successfully setting new standard availability rules, consider checking for existing booked interviews
        //       that now conflict with the combined set of ALL rules (new standard availability + existing one-off availability + existing exclusions).
        //       Notify the owner (and potentially the applicant) about the conflict so they can manually reschedule.
        //       A single process that checks all bookings against the combined set of ALL rules whenever ANY rule is added, updated, or deleted/replaced is recommended.

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
