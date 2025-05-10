using System.ComponentModel;
using Application.UseCases.Commands.Availability;
using Application.UseCases.Queries.Availability; // Added missing using for query inputs/outputs
using Domain.Constants;
using Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Presentation.API.Endpoints.Reviewer.Availability.Responses;
using Xcel.Services.Auth.Interfaces.Services;

namespace Presentation.API.Endpoints.Reviewer.Availability;

internal static class ReviewerAvailabilityEndpoints
{
    internal static IEndpointRouteBuilder MapReviewerAvailabilityEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        // Set recurring weekly availability rules (AvailabilityStandard)
        endpoints
            .MapPost(
                Endpoints.Reviewer.Availability.SetRecurring,
                async (
                    [FromBody] List<AvailabilityRuleInputRequest> rules,
                    IClientInfoService clientInfoService,
                    ISetAvailabilityRulesCommand command
                ) =>
                {
                    var input = rules
                        .Select(r => new AvailabilityRuleInput(
                            r.DayOfWeek,
                            r.StartTimeUtc,
                            r.EndTimeUtc,
                            r.ActiveFromUtc,
                            r.ActiveUntilUtc
                        ))
                        .ToList();

                    var result = await command.ExecuteAsync(
                        clientInfoService.UserId,
                        AvailabilityOwnerType.Reviewer,
                        input
                    );

                    return result.IsSuccess ? Results.Ok() : result.MapProblemDetails();
                }
            )
            .WithName("Reviewer.SetRecurringAvailability")
            .WithSummary("Define recurring weekly availability")
            .WithDescription(
                "Allows the reviewer to define a weekly recurring availability schedule (e.g., every Monday 9am–12pm). Replaces existing recurring availability."
            )
            .WithTags(UserRoles.Reviewer)
            .RequireAuthorization(p => p.RequireRole(UserRoles.Reviewer));

        // Add a one-off availability time slot (AvailabilityOneOff)
        endpoints
            .MapPost(
                Endpoints.Reviewer.Availability.AddOneOff,
                async (
                    [FromBody] OneOffAvailabilityInputRequest input,
                    IClientInfoService clientInfoService,
                    IAddOneOffAvailabilitySlotCommand command
                ) =>
                {
                    var result = await command.ExecuteAsync(
                        new OneOffAvailabilityInput(
                            clientInfoService.UserId,
                            AvailabilityOwnerType.Reviewer,
                            input.StartUtc,
                            input.EndUtc
                        )
                    );
                    return result.IsSuccess ? Results.Ok() : result.MapProblemDetails();
                }
            )
            .WithName("Reviewer.AddOneOffAvailability")
            .WithSummary("Add a one-time availability slot")
            .WithDescription(
                "Allows the reviewer to add a single availability slot that is not part of the standard recurring schedule."
            )
            .WithTags(UserRoles.Reviewer)
            .RequireAuthorization(p => p.RequireRole(UserRoles.Reviewer));

        // Add exclusion period (ExclusionFullDay or ExclusionTimeBased)
        endpoints
            .MapPost(
                Endpoints.Reviewer.Availability.AddExclusions,
                async (
                    [FromBody] ExclusionPeriodInputRequest input,
                    IClientInfoService clientInfoService,
                    IAddExclusionPeriodCommand command
                ) =>
                {
                    var result = await command.ExecuteAsync(
                        new ExclusionPeriodInput(
                            clientInfoService.UserId,
                            AvailabilityOwnerType.Reviewer,
                            input.StartDateUtc,
                            input.EndDateUtc,
                            input.Type,
                            input.StartTimeUtc,
                            input.EndTimeUtc
                        )
                    );
                    return result.IsSuccess ? Results.Ok() : result.MapProblemDetails();
                }
            )
            .WithName("Reviewer.AddExclusionPeriod")
            .WithSummary("Add exclusion dates or time periods")
            .WithDescription(
                "Marks specific dates (full day) or time periods on specific dates as unavailable."
            )
            .WithTags(UserRoles.Reviewer)
            .RequireAuthorization(p => p.RequireRole(UserRoles.Reviewer));

        // Delete a specific availability or exclusion rule (any type) - NEW ENDPOINT
        endpoints
            .MapDelete(
                Endpoints.Reviewer.Availability.DeleteRule, // Route includes {ruleId:guid}
                async (
                    Guid ruleId, // Captured from route
                    IClientInfoService clientInfoService,
                    IDeleteAvailabilityRuleCommand command
                ) =>
                {
                    var input = new DeleteAvailabilityRuleInput(
                        ruleId,
                        clientInfoService.UserId,
                        AvailabilityOwnerType.Reviewer
                    );

                    var result = await command.ExecuteAsync(input);

                    return result.IsSuccess ? Results.Ok() : result.MapProblemDetails();
                }
            )
            .WithName("Reviewer.DeleteAvailabilityRule")
            .WithSummary("Delete an availability or exclusion rule")
            .WithDescription("Allows the reviewer to delete a specific availability (standard or one-off) or exclusion rule by its ID.")
            .WithTags(UserRoles.Reviewer)
            .RequireAuthorization(p => p.RequireRole(UserRoles.Reviewer));


        // Get reviewer's availability rules (all types)
        endpoints
            .MapGet(
                Endpoints.Reviewer.Availability.GetRules,
                async (
                    IClientInfoService clientInfoService,
                    IGetAvailabilityRulesQuery query // Use the direct interface
                ) =>
                {
                    var result = await query.ExecuteAsync(
                        clientInfoService.UserId,
                        AvailabilityOwnerType.Reviewer
                    );

                    return result.IsSuccess
                        ? Results.Ok(new GetAvailabilityRulesResponse(result.Value.Select(ar => new AvailabilityRuleDto(
                            ar.Id,
                            ar.DayOfWeek,
                            ar.StartTimeUtc,
                            ar.EndTimeUtc,
                            ar.ActiveFromUtc,
                            ar.ActiveUntilUtc,
                            ar.RuleType
                        ))))
                        : result.MapProblemDetails();
                }
            )
            .WithName("Reviewer.GetAvailabilityRules")
            .WithSummary("Get reviewer's availability rules")
            .WithDescription("Returns all availability (standard and one-off) and exclusion (full-day and time-based) rules configured by the reviewer.")
            .WithTags(UserRoles.Reviewer)
            .RequireAuthorization(p => p.RequireRole(UserRoles.Reviewer));

        // Get calculated available slots based on all rules - NEW ENDPOINT
        endpoints
            .MapGet(
                Endpoints.Reviewer.Availability.GetSlots, // Route is /slots
                async (
                    [AsParameters] GetAvailabilitySlotsRequest request, // Bind query parameters to this record
                    IClientInfoService clientInfoService,
                    IGetAvailabilitySlotsQuery query // Use the direct interface
                ) =>
                {
                    var input = new AvailabilitySlotsQueryInput(
                        clientInfoService.UserId,
                        AvailabilityOwnerType.Reviewer,
                        request.FromUtc,
                        request.ToUtc,
                        request.SlotDuration
                    );

                    var result = await query.ExecuteAsync(input);

                    // result.Value is List<AvailableSlot>, which is a public record and suitable for direct return
                    return result.IsSuccess
                        ? Results.Ok(result.Value)
                        : result.MapProblemDetails();
                }
            )
            .WithName("Reviewer.GetAvailableSlots")
            .WithSummary("Get calculated available booking slots")
            .WithDescription("Calculates and returns specific bookable time slots for the reviewer within a date and time range, considering all availability and exclusion rules.")
            .WithTags(UserRoles.Reviewer)
            .RequireAuthorization(p => p.RequireRole(UserRoles.Reviewer));


        return endpoints;
    }

    // Request DTO for adding a single one-off availability slot (matches command input)
    public record OneOffAvailabilityInputRequest(
        [property: Description("Start time of the one-off availability slot (in UTC)")]
            DateTime StartUtc,
        [property: Description("End time of the one-off availability slot (in UTC)")]
            DateTime EndUtc
    );

    // Request DTO for setting standard availability rules (matches command input, IsExcluded removed)
    public record AvailabilityRuleInputRequest(
        [property: Description("Day of the week when the availability rule applies")]
            DayOfWeek DayOfWeek,
        [property: Description("Start time of the availability slot on that day (in UTC)")]
            TimeSpan StartTimeUtc,
        [property: Description("End time of the availability slot on that day (in UTC)")]
            TimeSpan EndTimeUtc,
        [property: Description("Start date when this rule becomes active (in UTC)")]
            DateTime ActiveFromUtc,
        [property: Description("Optional end date when this rule stops being active (in UTC)")]
            DateTime? ActiveUntilUtc = null
        // IsExcluded is removed from this request DTO
    );

    // Request DTO for adding exclusion periods (matches command input)
    public record ExclusionPeriodInputRequest(
        [property: Description("Start date of the exclusion period (in UTC)")]
            DateTime StartDateUtc,
        [property: Description("End date of the exclusion period (in UTC)")]
            DateTime EndDateUtc,
        [property: Description("Type of exclusion (FullDay or SpecificTime)")]
            ExclusionType Type, // Added Type
        [property: Description("Start time of the exclusion (in UTC). Required if Type is SpecificTime.")]
            TimeSpan? StartTimeUtc = null, // Added StartTimeUtc
        [property: Description("End time of the exclusion (in UTC). Required if Type is SpecificTime.")]
            TimeSpan? EndTimeUtc = null // Added EndTimeUtc
    );

     // Request DTO for getting calculated available slots (for query parameters) - NEW DTO
    public record GetAvailabilitySlotsRequest(
        [property: FromQuery(Name = "fromUtc"), Description("The inclusive start date and time for the availability search (in UTC)")]
        DateTime FromUtc,
        [property: FromQuery(Name = "toUtc"), Description("The inclusive end date and time for the availability search (in UTC)")]
        DateTime ToUtc,
        [property: FromQuery(Name = "duration"), Description("The desired duration for each availability slot (e.g., '00:30:00' for 30 minutes)")]
        TimeSpan SlotDuration
    );
}