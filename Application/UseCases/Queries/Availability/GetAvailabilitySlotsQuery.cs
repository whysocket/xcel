namespace Application.UseCases.Queries.Availability;

/// <summary>
/// Returns available time slots (e.g. 30 minutes each) for a given person
/// based on their availability rules and exclusions.
/// Correctly consolidates availability and subtracts exclusion periods (full-day and time-based)
/// using the RuleType enum.
/// </summary>
public interface IGetAvailabilitySlotsQuery
{
    Task<Result<List<AvailableSlot>>> ExecuteAsync(
        AvailabilitySlotsQueryInput input,
        CancellationToken cancellationToken = default
    );
}

public record AvailabilitySlotsQueryInput(
    Guid OwnerId,
    AvailabilityOwnerType OwnerType,
    DateTime FromUtc,
    DateTime ToUtc,
    TimeSpan SlotDuration
);

public record AvailableSlot(DateTime StartUtc, DateTime EndUtc);

internal sealed class GetAvailabilitySlotsQuery(
    IAvailabilityRulesRepository repository, // Assuming IAvailabilityRulesRepository exists
    ILogger<GetAvailabilitySlotsQuery> logger
) : IGetAvailabilitySlotsQuery
{
    private const string ServiceName = "[GetAvailabilitySlotsQuery]";

    public async Task<Result<List<AvailableSlot>>> ExecuteAsync(
        AvailabilitySlotsQueryInput input,
        CancellationToken cancellationToken = default
    )
    {
        // Ensure input date range is UTC and contains only Date part for consistent comparison in repository fetch
        var fromDateOnlyUtc = input.FromUtc.Date;
        var toDateOnlyUtc = input.ToUtc.Date;

        logger.LogInformation(
            "{Service} Generating {SlotDuration} slots for {OwnerType} {OwnerId} between {From:yyyy-MM-dd HH:mm} and {To:yyyy-MM-dd HH:mm}. Date range for rule fetch: {FromDateOnly:yyyy-MM-dd} to {ToDateOnly:yyyy-MM-dd}",
            ServiceName,
            input.SlotDuration,
            input.OwnerType,
            input.OwnerId,
            input.FromUtc, // Log full FromUtc and ToUtc from input
            input.ToUtc,
            fromDateOnlyUtc, // Log date-only range used for repository fetch
            toDateOnlyUtc
        );

        // Fetch all potentially relevant rules whose ActiveFromUtc/ActiveUntilUtc ranges
        // overlap with the *date part* of the input range.
        var rulesInRange = await repository.GetByOwnerAndDateRangeAsync(
            input.OwnerId,
            input.OwnerType,
            fromDateOnlyUtc,
            toDateOnlyUtc,
            cancellationToken
        );

        var availableSlots = new List<AvailableSlot>();
        var currentUtc = DateTime.UtcNow;

        // Iterate through each day in the requested date range (using the Date part from the input)
        // This ensures we process every calendar day requested, even if the time range is partial.
        for (var date = fromDateOnlyUtc; date <= toDateOnlyUtc; date = date.AddDays(1))
        {
            logger.LogTrace("{Service} Processing date: {Date:yyyy-MM-dd}", ServiceName, date);

            // Get rules from the fetched set that are actually active on THIS specific date.
            // This filters by DayOfWeek and confirms the rule's ActiveFrom/Until dates include 'date'.
            var activeRulesForDay = GetRulesActiveOnDate(rulesInRange, date);

            if (!activeRulesForDay.Any())
            {
                logger.LogTrace(
                    "{Service} No rules active on {Date:yyyy-MM-dd}. Skipping slot generation.",
                    ServiceName,
                    date
                );
                continue; // No rules applicable to this day, move to the next
            }

            // Separate active rules into availability and exclusion lists based on RuleType
            var availabilityRules = activeRulesForDay
                .Where(r =>
                    r.RuleType == AvailabilityRuleType.AvailabilityStandard
                    || r.RuleType == AvailabilityRuleType.AvailabilityOneOff
                )
                .ToList();

            var exclusionRules = activeRulesForDay
                .Where(r =>
                    r.RuleType == AvailabilityRuleType.ExclusionFullDay
                    || r.RuleType == AvailabilityRuleType.ExclusionTimeBased
                )
                .ToList();

            logger.LogTrace(
                "{Service} Found {AvailCount} availability rules and {ExclCount} exclusion rules for {Date:yyyy-MM-dd}",
                ServiceName,
                availabilityRules.Count,
                exclusionRules.Count,
                date
            );

            // Check for full-day exclusions first
            if (exclusionRules.Any(r => r.RuleType == AvailabilityRuleType.ExclusionFullDay))
            {
                logger.LogTrace(
                    "{Service} Full-day exclusion found for {Date:yyyy-MM-dd}. Skipping slot generation.",
                    ServiceName,
                    date
                );
                continue; // The entire day is blocked by a full-day exclusion
            }

            // Calculate consolidated available time intervals for this day
            var availableIntervals = ConsolidateTimeIntervals(availabilityRules);
            logger.LogTrace(
                "{Service} Consolidated available intervals for {Date:yyyy-MM-dd}: {@Intervals}",
                ServiceName,
                date,
                availableIntervals
            );

            // Calculate consolidated excluded time intervals for this day (only TimeBased Exclusions)
            var excludedIntervals = ConsolidateTimeIntervals(
                exclusionRules
                    .Where(r => r.RuleType == AvailabilityRuleType.ExclusionTimeBased)
                    .ToList()
            );
            logger.LogTrace(
                "{Service} Consolidated excluded intervals for {Date:yyyy-MM-dd}: {@Intervals}",
                ServiceName,
                date,
                excludedIntervals
            );

            // Subtract excluded intervals from available intervals to get net bookable intervals for the day
            var netBookableIntervals = SubtractIntervals(availableIntervals, excludedIntervals);
            logger.LogTrace(
                "{Service} Net bookable intervals for {Date:yyyy-MM-dd}: {@Intervals}",
                ServiceName,
                date,
                netBookableIntervals
            );

            // Generate slots within the net bookable intervals, respecting the overall input date/time range and UtcNow
            foreach (var interval in netBookableIntervals)
            {
                // Calculate the absolute UTC start and end time for the current interval on this specific date
                var intervalStartUtc = date + interval.Start;
                var intervalEndUtc = date + interval.End;

                // Determine the effective start and end time for slot generation for this interval,
                // considering the overall query range and the current time (UtcNow).
                // Slots cannot start before the query's FromUtc or before the interval starts.
                // Use ternary operator for DateTime comparison instead of Math.Max
                var effectiveIntervalStart =
                    intervalStartUtc > input.FromUtc ? intervalStartUtc : input.FromUtc;

                // Slots cannot end after the query's ToUtc or after the interval ends.
                // Use ternary operator for DateTime comparison instead of Math.Min
                var effectiveIntervalEnd =
                    intervalEndUtc < input.ToUtc ? intervalEndUtc : input.ToUtc;

                // Ensure the starting point for the slot generation loop is not in the past relative to UtcNow
                // Use ternary operator for DateTime comparison instead of Math.Max
                var slotLoopStart =
                    effectiveIntervalStart > currentUtc ? effectiveIntervalStart : currentUtc;

                logger.LogTrace(
                    "{Service} Generating slots for interval {Interval} on {Date:yyyy-MM-dd}. Effective range: {EffectiveStart:HH:mm:ss} to {EffectiveEnd:HH:mm:ss}. Loop starts at {LoopStart:HH:mm:ss}",
                    ServiceName,
                    interval,
                    date,
                    effectiveIntervalStart.TimeOfDay,
                    effectiveIntervalEnd.TimeOfDay,
                    slotLoopStart.TimeOfDay
                );

                // Generate slots within the effective interval range
                // The loop continues as long as the *end* time of the potential slot is within the effective end time.
                for (
                    var time = slotLoopStart;
                    time.Add(input.SlotDuration) <= effectiveIntervalEnd;
                    time = time.Add(input.SlotDuration)
                )
                {
                    logger.LogTrace(
                        "{Service} Adding slot: {SlotStart:HH:mm:ss} to {SlotEnd:HH:mm:ss}",
                        ServiceName,
                        time.TimeOfDay, // .TimeOfDay should now be resolvable
                        time.Add(input.SlotDuration).TimeOfDay // .TimeOfDay should now be resolvable
                    );
                    availableSlots.Add(new AvailableSlot(time, time.Add(input.SlotDuration)));
                }
            }
        }

        logger.LogInformation(
            "{Service} Generated {Count} slots for {OwnerId} for query range {From:yyyy-MM-dd HH:mm} to {To:yyyy-MM-dd HH:mm}",
            ServiceName,
            availableSlots.Count,
            input.OwnerId,
            input.FromUtc,
            input.ToUtc
        );

        return Result.Ok(availableSlots);
    }

    /// <summary>
    /// Filters a list of rules to find those active on a specific date based on DayOfWeek and ActiveFrom/Until dates.
    /// Includes all RuleTypes active on the given date.
    /// </summary>
    private static List<AvailabilityRule> GetRulesActiveOnDate(
        List<AvailabilityRule> rules,
        DateTime date
    )
    {
        var compareDate = date.Date;
        var dayOfWeek = date.DayOfWeek;

        return rules
            .Where(r =>
                r.DayOfWeek == dayOfWeek
                && compareDate >= r.ActiveFromUtc.Date
                && (r.ActiveUntilUtc == null || compareDate <= r.ActiveUntilUtc.Value.Date)
            )
            .ToList();
    }

    /// <summary>
    /// Consolidates a list of AvailabilityRule time ranges into a list of non-overlapping intervals.
    /// Assumes rules in the input list are all of a type that has meaningful time ranges (e.g., AvailabilityStandard, AvailabilityOneOff, ExclusionTimeBased).
    /// </summary>
    private static List<(TimeSpan Start, TimeSpan End)> ConsolidateTimeIntervals(
        List<AvailabilityRule>? rules
    )
    {
        if (rules == null || !rules.Any())
        {
            return [];
        }

        // Sort rules by start time
        var sortedIntervals = rules
            // Ensure EndTime is strictly after StartTime before processing
            .Where(r => r.StartTimeUtc < r.EndTimeUtc)
            .Select(r => (Start: r.StartTimeUtc, End: r.EndTimeUtc))
            .OrderBy(i => i.Start)
            .ToList();

        if (!sortedIntervals.Any())
        {
            return [];
        }

        var consolidated = new List<(TimeSpan Start, TimeSpan End)>();
        var current = sortedIntervals.First();

        foreach (var interval in sortedIntervals.Skip(1))
        {
            // If the current interval overlaps with or touches the next one, merge them
            // The new end time is the later of the two current end times if there is overlap or touching end time
            if (interval.Start <= current.End)
            {
                current = (
                    current.Start,
                    TimeSpan.FromTicks(Math.Max(current.End.Ticks, interval.End.Ticks))
                );
            }
            else
            {
                // No overlap, add the current interval and start a new one
                consolidated.Add(current);
                current = interval;
            }
        }
        consolidated.Add(current); // Add the last interval

        return consolidated;
    }

    /// <summary>
    /// Subtracts a list of excluded time intervals from a list of available time intervals.
    /// Assumes both input lists (available and excluded) are already sorted and consolidated.
    /// Returns a list of net bookable intervals.
    /// </summary>
    private static List<(TimeSpan Start, TimeSpan End)> SubtractIntervals(
        List<(TimeSpan Start, TimeSpan End)> available,
        List<(TimeSpan Start, TimeSpan End)> excluded
    )
    {
        if (!available.Any())
        {
            return []; // No availability means no bookable slots
        }
        if (!excluded.Any())
        {
            return available; // No exclusions means all available intervals are bookable
        }

        var result = new List<(TimeSpan Start, TimeSpan End)>();
        var currentAvailableIndex = 0;
        var currentExcludedIndex = 0;

        // Iterate through available intervals
        while (currentAvailableIndex < available.Count)
        {
            var avail = available[currentAvailableIndex];

            // Iterate through excluded intervals that might overlap with the current available interval
            while (
                currentExcludedIndex < excluded.Count
                && excluded[currentExcludedIndex].End <= avail.Start
            )
            {
                // Excluded interval ends before or at the start of the current available interval. Move to next exclusion.
                currentExcludedIndex++;
            }

            // Now, excluded[currentExcludedIndex] (if it exists) is the first exclusion that might overlap avail.
            while (
                currentExcludedIndex < excluded.Count
                && excluded[currentExcludedIndex].Start < avail.End
            )
            {
                var excl = excluded[currentExcludedIndex];

                // Add the segment of the available interval *before* the current excluded interval (if any)
                if (avail.Start < excl.Start)
                {
                    result.Add((avail.Start, excl.Start));
                }

                // Update the start of the available interval to the end of the excluded interval.
                // This effectively removes the overlapping part.
                avail = (
                    TimeSpan.FromTicks(Math.Max(avail.Start.Ticks, excl.End.Ticks)),
                    avail.End
                );

                // If the remaining part of the available interval is now empty or reversed,
                // the current available interval is fully processed by this exclusion. Break the inner loop.
                if (avail.Start >= avail.End)
                {
                    break; // Exit inner while loop (processing exclusions for the current avail)
                }

                // Move to the next excluded interval to check against the remaining part of the current available interval.
                currentExcludedIndex++;
            }

            // After checking all relevant exclusions for the current available interval, if there's a remaining part, add it.
            if (avail.Start < avail.End)
            {
                result.Add(avail);
            }
            // Note: If no exclusions overlapped and avail.Start < avail.End initially, the entire avail interval is added here.
            // If exclusions fully covered avail or truncated it to zero length, nothing is added here unless segments before exclusions were added.

            currentAvailableIndex++; // Move to the next available interval
        }

        // The result list contains the net bookable intervals. No need to filter zero-length intervals here if the logic above is correct.
        return result;
    }

    /// <summary>
    /// Represents a time range with Start and End TimeSpans.
    /// Used internally for interval calculations.
    /// </summary>
    private readonly record struct TimeSpanInterval(TimeSpan Start, TimeSpan End);
}
