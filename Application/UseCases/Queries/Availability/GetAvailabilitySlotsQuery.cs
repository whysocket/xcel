namespace Application.UseCases.Queries.Availability;

/// <summary>
/// Returns available time slots (e.g. 30 minutes each) for a given person
/// based on their availability rules and exclusions.
/// </summary>
public interface IGetAvailabilitySlotsQuery
{
    Task<Result<List<AvailableSlot>>> ExecuteAsync(AvailabilitySlotsQueryInput input, CancellationToken cancellationToken = default);
}

public record AvailabilitySlotsQueryInput(
    Guid OwnerId,
    AvailabilityOwnerType OwnerType,
    DateTime FromUtc,
    DateTime ToUtc,
    TimeSpan SlotDuration);

public record AvailableSlot(DateTime StartUtc, DateTime EndUtc);

internal sealed class GetAvailabilitySlotsQuery(
    IAvailabilityRulesRepository repository,
    ILogger<GetAvailabilitySlotsQuery> logger
) : IGetAvailabilitySlotsQuery
{
    private const string ServiceName = "[GetAvailabilitySlotsQuery]";

    public async Task<Result<List<AvailableSlot>>> ExecuteAsync(
        AvailabilitySlotsQueryInput input,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("{Service} Generating slots for {OwnerType} {OwnerId} between {From} and {To}",
            ServiceName, input.OwnerType, input.OwnerId, input.FromUtc, input.ToUtc);

        var rules = await repository.GetByOwnerAndDateRangeAsync(input.OwnerId, input.OwnerType, input.FromUtc.Date,
            input.ToUtc.Date, cancellationToken);
        var availableSlots = new List<AvailableSlot>();

        for (var date = input.FromUtc.Date; date <= input.ToUtc.Date; date = date.AddDays(1))
        {
            var dayRules = rules
                .Where(r =>
                    !r.IsExcluded &&
                    r.DayOfWeek == date.DayOfWeek &&
                    date >= r.ActiveFromUtc.Date &&
                    (r.ActiveUntilUtc == null || date <= r.ActiveUntilUtc.Value.Date))
                .ToList();

            foreach (var rule in dayRules)
            {
                var slotStart = date + rule.StartTimeUtc;
                var slotEnd = date + rule.EndTimeUtc;

                for (var time = slotStart; time.Add(input.SlotDuration) <= slotEnd; time = time.Add(input.SlotDuration))
                {
                    availableSlots.Add(new AvailableSlot(time, time.Add(input.SlotDuration)));
                }
            }
        }

        logger.LogInformation("{Service} Generated {Count} slots", ServiceName, availableSlots.Count);
        return Result.Ok(availableSlots);
    }
}
