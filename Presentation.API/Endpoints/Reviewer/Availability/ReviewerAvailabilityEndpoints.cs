using System.ComponentModel;
using Application.UseCases.Commands.Availability;
using Domain.Constants;
using Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Xcel.Services.Auth.Interfaces.Services;

namespace Presentation.API.Endpoints.Reviewer.Availability;

internal static class ReviewerAvailabilityEndpoints
{
    internal static IEndpointRouteBuilder MapReviewerAvailabilityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // Set recurring weekly availability rules
        endpoints.MapPost(Endpoints.Reviewer.Availability.SetRecurring,
                async (
                    [FromBody] List<AvailabilityRuleInputRequest> rules, 
                    IClientInfoService clientInfoService,
                    ISetAvailabilityRulesCommand command) =>
                {
                    var input = rules.Select(r => new AvailabilityRuleInput(
                        r.DayOfWeek,
                        r.StartTimeUtc,
                        r.EndTimeUtc,
                        r.ActiveFromUtc,
                        r.ActiveUntilUtc,
                        r.IsExcluded)).ToList();

                    var result = await command.ExecuteAsync(clientInfoService.UserId, AvailabilityOwnerType.Reviewer, input);
                    return result.IsSuccess ? Results.Ok() : result.MapProblemDetails();
                })
            .WithName("Reviewer.SetRecurringAvailability")
            .WithSummary("Define recurring weekly availability")
            .WithDescription("Allows the reviewer to define a weekly recurring availability schedule (e.g., every Monday 9am–12pm).")
            .WithTags(UserRoles.Reviewer)
            .RequireAuthorization(p => p.RequireRole(UserRoles.Reviewer));

        // Add a one-off availability time slot
        endpoints.MapPost(Endpoints.Reviewer.Availability.AddOneOff,
                async (
                    [FromBody] OneOffAvailabilityInputRequest input,
                    IClientInfoService clientInfoService,
                    IAddOneOffAvailabilitySlotCommand command) =>
                {
                    var result = await command.ExecuteAsync(new(
                        clientInfoService.UserId,
                        AvailabilityOwnerType.Reviewer,
                        input.StartUtc,
                        input.EndUtc));
                    return result.IsSuccess ? Results.Ok() : result.MapProblemDetails();
                })
            .WithName("Reviewer.AddOneOffAvailability")
            .WithSummary("Add a one-time availability slot")
            .WithDescription("Allows the reviewer to add a single availability slot that is not part of the recurring schedule.")
            .WithTags(UserRoles.Reviewer)
            .RequireAuthorization(p => p.RequireRole(UserRoles.Reviewer));

        // Add exclusion period (e.g., holidays)
        endpoints.MapPost(Endpoints.Reviewer.Availability.AddExclusions,
                async (
                    [FromBody] ExclusionPeriodInputRequest input,
                    IClientInfoService clientInfoService,
                    IAddExclusionPeriodCommand command) =>
                {
                    var result = await command.ExecuteAsync(new(
                        clientInfoService.UserId,
                        AvailabilityOwnerType.Reviewer,
                        input.StartDateUtc,
                        input.EndDateUtc));
                    return result.IsSuccess ? Results.Ok() : result.MapProblemDetails();
                })
            .WithName("Reviewer.AddExclusionPeriod")
            .WithSummary("Add exclusion dates")
            .WithDescription("Marks specific dates as unavailable for interviews (e.g., public holidays, vacation days).")
            .WithTags(UserRoles.Reviewer)
            .RequireAuthorization(p => p.RequireRole(UserRoles.Reviewer));

        return endpoints;
    }

    public record OneOffAvailabilityInputRequest(
        [property: Description("Start time of the one-off availability slot (in UTC)")] DateTime StartUtc,
        [property: Description("End time of the one-off availability slot (in UTC)")] DateTime EndUtc);

    public record AvailabilityRuleInputRequest(
        [property: Description("Day of the week when the availability rule applies")] DayOfWeek DayOfWeek,
        [property: Description("Start time of the availability slot on that day (in UTC)")] TimeSpan StartTimeUtc,
        [property: Description("End time of the availability slot on that day (in UTC)")] TimeSpan EndTimeUtc,
        [property: Description("Start date when this rule becomes active (in UTC)")] DateTime ActiveFromUtc,
        [property: Description("Optional end date when this rule stops being active (in UTC)")] DateTime? ActiveUntilUtc = null,
        [property: Description("Whether this rule should mark the day as unavailable (e.g. exclusion)")] bool IsExcluded = false);

    public record ExclusionPeriodInputRequest(
        [property: Description("Start date of the exclusion period (in UTC)")] DateTime StartDateUtc,
        [property: Description("End date of the exclusion period (in UTC)")] DateTime EndDateUtc);
}