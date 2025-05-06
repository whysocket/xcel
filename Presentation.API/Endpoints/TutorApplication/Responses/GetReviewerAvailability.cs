using System.ComponentModel;
using Application.UseCases.Queries.TutorApplicationOnboarding.Applicant.Step3;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.API.Endpoints.TutorApplication.Responses;

public record GetReviewerAvailabilityRequest(
    [FromQuery]
    [property: Description("The UTC date for which to fetch the reviewer's available time slots.")]
        DateOnly DateUtc
);

public record GetReviewerAvailabilityResponse(
    [property: Description("List of available time slots for the specified UTC date.")]
        IEnumerable<TimeSlotDto> Slots
)
{
    public static GetReviewerAvailabilityResponse FromDomain(IEnumerable<TimeSlot> timeSlots) =>
        new(timeSlots.Select(TimeSlotDto.FromDomain));
}

public record TimeSlotDto(
    [property: Description("Start of the available time slot in UTC.")] DateTime StartUtc,
    [property: Description("End of the available time slot in UTC.")] DateTime EndUtc
)
{
    public static TimeSlotDto FromDomain(TimeSlot slot) => new(slot.StartUtc, slot.EndUtc);
}
