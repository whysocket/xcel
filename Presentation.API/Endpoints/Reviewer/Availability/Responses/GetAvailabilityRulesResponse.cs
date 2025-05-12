using Domain.Entities;

namespace Presentation.API.Endpoints.Reviewer.Availability.Responses;

public record AvailabilityRuleDto(
    Guid Id,
    DayOfWeek DayOfWeek,
    TimeSpan StartTimeUtc,
    TimeSpan EndTimeUtc,
    DateTime ActiveFromUtc,
    DateTime? ActiveUntilUtc,
    AvailabilityRuleType RuleType
);

public record GetAvailabilityRulesResponse(IEnumerable<AvailabilityRuleDto> Rules);
