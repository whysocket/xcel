using Application.UseCases.Queries.Availability;
using Domain.Entities;
using Xcel.TestUtils;

namespace Domain.IntegrationTests.UseCases.Queries.Availability;

public class GetAvailabilitySlotsQueryTests : BaseTest
{
    private IGetAvailabilitySlotsQuery _query = null!;
    private Person _testOwner = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _query = new GetAvailabilitySlotsQuery(
            AvailabilityRulesRepository,
            CreateLogger<GetAvailabilitySlotsQuery>()
        );

        // Create a person to own the rules for most tests
        _testOwner = new Person
        {
            FirstName = "Slot",
            LastName = "Owner",
            EmailAddress = "slot.owner@xcel.com",
            Id = Guid.NewGuid(),
        };
        await PersonsRepository.AddAsync(_testOwner);
        await PersonsRepository.SaveChangesAsync();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldGenerateSlots_ForBasicRecurringAvailability()
    {
        // Scenario: Query for slots within a simple recurring availability rule.
        // Arrange
        var startDate = FakeTimeProvider.GetUtcNow().UtcDateTime.Date;
        var mondayRule = new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            OwnerId = _testOwner.Id,
            Owner = _testOwner,
            OwnerType = AvailabilityOwnerType.Reviewer,
            DayOfWeek = DayOfWeek.Monday,
            StartTimeUtc = TimeSpan.FromHours(9), // 9:00 AM
            EndTimeUtc = TimeSpan.FromHours(12), // 12:00 PM
            ActiveFromUtc = startDate,
            ActiveUntilUtc = null,
            IsExcluded = false, // Availability
        };
        await AvailabilityRulesRepository.AddAsync(mondayRule);
        await AvailabilityRulesRepository.SaveChangesAsync();

        // Find the first Monday on or after startDate
        var firstMonday = startDate;
        while (firstMonday.DayOfWeek != DayOfWeek.Monday)
        {
            firstMonday = firstMonday.AddDays(1);
        }

        // Query for that specific Monday (from midnight to end of day)
        var input = new AvailabilitySlotsQueryInput(
            OwnerId: _testOwner.Id,
            OwnerType: AvailabilityOwnerType.Reviewer,
            FromUtc: firstMonday.Date,
            ToUtc: firstMonday.Date.AddDays(1).AddTicks(-1),
            SlotDuration: TimeSpan.FromMinutes(30) // 30 minute slots
        );

        // Act
        var result = await _query.ExecuteAsync(input);

        // Assert
        Assert.True(result.IsSuccess);
        var slots = result.Value;

        // Expected slots: 9:00-9:30, 9:30-10:00, 10:00-10:30, 10:30-11:00, 11:00-11:30, 11:30-12:00
        Assert.Equal(6, slots.Count);
        Assert.Equal(firstMonday.Date.AddHours(9), slots[0].StartUtc);
        Assert.Equal(firstMonday.Date.AddHours(9.5), slots[0].EndUtc);
        Assert.Equal(firstMonday.Date.AddHours(11.5), slots[5].StartUtc);
        Assert.Equal(firstMonday.Date.AddHours(12), slots[5].EndUtc);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldGenerateSlots_ForMultipleRulesOnSameDay()
    {
        // Scenario: Query for slots on a day with split availability rules (e.g., before and after lunch).
        // Arrange
        var startDate = FakeTimeProvider.GetUtcNow().UtcDateTime.Date;
        var ownerId = _testOwner.Id;
        var ownerType = AvailabilityOwnerType.Tutor;

        // Add two rules for the same day (Tuesday)
        var tuesdayMorning = new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Owner = _testOwner,
            OwnerType = ownerType,
            DayOfWeek = DayOfWeek.Tuesday,
            StartTimeUtc = TimeSpan.FromHours(9),
            EndTimeUtc = TimeSpan.FromHours(10), // 9:00-10:00
            ActiveFromUtc = startDate,
            ActiveUntilUtc = null,
            IsExcluded = false,
        };
        var tuesdayAfternoon = new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Owner = _testOwner,
            OwnerType = ownerType,
            DayOfWeek = DayOfWeek.Tuesday,
            StartTimeUtc = TimeSpan.FromHours(11),
            EndTimeUtc = TimeSpan.FromHours(12), // 11:00-12:00
            ActiveFromUtc = startDate,
            ActiveUntilUtc = null,
            IsExcluded = false,
        };
        await AvailabilityRulesRepository.AddRangeAsync(new[] { tuesdayMorning, tuesdayAfternoon });
        await AvailabilityRulesRepository.SaveChangesAsync();

        // Find the first Tuesday on or after startDate
        var firstTuesday = startDate;
        while (firstTuesday.DayOfWeek != DayOfWeek.Tuesday)
        {
            firstTuesday = firstTuesday.AddDays(1);
        }

        // Query for that specific Tuesday (from midnight to end of day)
        var input = new AvailabilitySlotsQueryInput(
            OwnerId: ownerId,
            OwnerType: ownerType,
            FromUtc: firstTuesday.Date,
            ToUtc: firstTuesday.Date.AddDays(1).AddTicks(-1),
            SlotDuration: TimeSpan.FromMinutes(30)
        );

        // Act
        var result = await _query.ExecuteAsync(input);

        // Assert
        Assert.True(result.IsSuccess);
        var slots = result.Value;

        // Expected slots: 9:00-9:30, 9:30-10:00 (from first rule) AND 11:00-11:30, 11:30-12:00 (from second rule)
        Assert.Equal(4, slots.Count);
        var slotTimes = slots.Select(s => s.StartUtc.TimeOfDay).OrderBy(t => t).ToList();

        Assert.Contains(TimeSpan.FromHours(9), slotTimes);
        Assert.Contains(TimeSpan.FromHours(9.5), slotTimes);
        Assert.Contains(TimeSpan.FromHours(11), slotTimes);
        Assert.Contains(TimeSpan.FromHours(11.5), slotTimes);

        // Verify the correct dates
        Assert.All(slots, s => Assert.Equal(firstTuesday.Date, s.StartUtc.Date));
    }

    [Fact]
    public async Task ExecuteAsync_ShouldGenerateSlots_IgnoringRulesOutsideActiveFromDateRange()
    {
        // Scenario: Query for slots before a rule becomes active.
        // Arrange
        var today = FakeTimeProvider.GetUtcNow().UtcDateTime.Date;
        var activeFromDate = today.AddDays(10); // Rule starts 10 days from now
        var ownerId = _testOwner.Id;
        var ownerType = AvailabilityOwnerType.Reviewer;

        var futureRule = new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Owner = _testOwner,
            OwnerType = ownerType,
            DayOfWeek = DayOfWeek.Wednesday,
            StartTimeUtc = TimeSpan.FromHours(9),
            EndTimeUtc = TimeSpan.FromHours(10),
            ActiveFromUtc = activeFromDate,
            ActiveUntilUtc = null,
            IsExcluded = false,
        };
        await AvailabilityRulesRepository.AddAsync(futureRule);
        await AvailabilityRulesRepository.SaveChangesAsync();

        // Query for a date *before* the rule is active (e.g., 5 days from now)
        var queryDate = today.AddDays(5);

        var input = new AvailabilitySlotsQueryInput(
            OwnerId: ownerId,
            OwnerType: ownerType,
            FromUtc: queryDate.Date,
            ToUtc: queryDate.Date.AddDays(1).AddTicks(-1),
            SlotDuration: TimeSpan.FromMinutes(30)
        );

        // Act
        var result = await _query.ExecuteAsync(input);

        // Assert
        Assert.True(result.IsSuccess);
        var slots = result.Value;

        // Expected: No slots, because the rule is not yet active on queryDate
        Assert.Empty(slots);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldGenerateSlots_IgnoringRulesOutsideActiveUntilDateRange()
    {
        // Scenario: Query for slots after a rule has expired.
        // Arrange
        var today = FakeTimeProvider.GetUtcNow().UtcDateTime.Date;
        var activeUntilDate = today.AddDays(10); // Rule ends 10 days from now
        var ownerId = _testOwner.Id;
        var ownerType = AvailabilityOwnerType.Tutor;

        var expiringRule = new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Owner = _testOwner,
            OwnerType = ownerType,
            DayOfWeek = DayOfWeek.Thursday,
            StartTimeUtc = TimeSpan.FromHours(14),
            EndTimeUtc = TimeSpan.FromHours(15),
            ActiveFromUtc = today,
            ActiveUntilUtc = activeUntilDate, // Rule expires
            IsExcluded = false,
        };
        await AvailabilityRulesRepository.AddAsync(expiringRule);
        await AvailabilityRulesRepository.SaveChangesAsync();

        // Query for a date *after* the rule has expired (e.g., 15 days from now)
        var queryDate = today.AddDays(15);

        var input = new AvailabilitySlotsQueryInput(
            OwnerId: ownerId,
            OwnerType: ownerType,
            FromUtc: queryDate.Date,
            ToUtc: queryDate.Date.AddDays(1).AddTicks(-1),
            SlotDuration: TimeSpan.FromMinutes(30)
        );

        // Act
        var result = await _query.ExecuteAsync(input);

        // Assert
        Assert.True(result.IsSuccess);
        var slots = result.Value;

        // Expected: No slots, because the rule is no longer active on queryDate
        Assert.Empty(slots);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldGenerateSlots_OnlyOnCorrectDaysOfWeekInDateRange()
    {
        // Scenario: Query for a date range spanning multiple days, but expect slots only on the specific DayOfWeek defined in the rule.
        // Arrange
        var startDate = FakeTimeProvider.GetUtcNow().UtcDateTime.Date;
        var ownerId = _testOwner.Id;
        var ownerType = AvailabilityOwnerType.Reviewer;

        var mondayRule = new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Owner = _testOwner,
            OwnerType = ownerType,
            DayOfWeek = DayOfWeek.Monday,
            StartTimeUtc = TimeSpan.FromHours(9),
            EndTimeUtc = TimeSpan.FromHours(10),
            ActiveFromUtc = startDate,
            ActiveUntilUtc = null,
            IsExcluded = false,
        };
        var wednesdayRule = new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Owner = _testOwner,
            OwnerType = ownerType,
            DayOfWeek = DayOfWeek.Wednesday,
            StartTimeUtc = TimeSpan.FromHours(14),
            EndTimeUtc = TimeSpan.FromHours(15),
            ActiveFromUtc = startDate,
            ActiveUntilUtc = null,
            IsExcluded = false,
        };
        await AvailabilityRulesRepository.AddRangeAsync(new[] { mondayRule, wednesdayRule });
        await AvailabilityRulesRepository.SaveChangesAsync();

        // Find the upcoming week starting from startDate
        var queryFromDate = startDate;
        var queryToDate = queryFromDate.AddDays(6); // Query for a full week

        var input = new AvailabilitySlotsQueryInput(
            OwnerId: ownerId,
            OwnerType: ownerType,
            FromUtc: queryFromDate.Date,
            ToUtc: queryToDate.Date.AddDays(1).AddTicks(-1),
            SlotDuration: TimeSpan.FromMinutes(30)
        );

        // Act
        var result = await _query.ExecuteAsync(input);

        // Assert
        Assert.True(result.IsSuccess);
        var slots = result.Value;

        // Expected: Slots generated only for the Monday and Wednesday within the query date range
        // Monday rule: 9:00-9:30, 9:30-10:00 (2 slots)
        // Wednesday rule: 14:00-14:30, 14:30-15:00 (2 slots)
        Assert.Equal(4, slots.Count);

        // Verify the days of the week for the generated slots
        Assert.All(slots.Take(2), s => Assert.Equal(DayOfWeek.Monday, s.StartUtc.DayOfWeek));
        Assert.All(slots.Skip(2), s => Assert.Equal(DayOfWeek.Wednesday, s.StartUtc.DayOfWeek));
    }

    [Fact]
    public async Task ExecuteAsync_ShouldGenerateSlots_IncludingOneOffAvailability()
    {
        // Scenario: Query for slots including a one-off availability rule.
        // Arrange
        var today = FakeTimeProvider.GetUtcNow().UtcDateTime.Date;
        var oneOffDate = today.AddDays(7); // One week from now
        var ownerId = _testOwner.Id;
        var ownerType = AvailabilityOwnerType.Tutor;

        // Add a one-off rule
        var oneOffRule = new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Owner = _testOwner,
            OwnerType = ownerType,
            DayOfWeek = oneOffDate.DayOfWeek, // DayOfWeek must match the one-off date's DayOfWeek
            StartTimeUtc = TimeSpan.FromHours(10), // 10:00 AM
            EndTimeUtc = TimeSpan.FromHours(11), // 11:00 AM
            ActiveFromUtc = oneOffDate,
            ActiveUntilUtc = oneOffDate, // One-off date
            IsExcluded = false, // Availability
        };
        await AvailabilityRulesRepository.AddAsync(oneOffRule);
        await AvailabilityRulesRepository.SaveChangesAsync();

        // Query for that specific one-off date (from midnight to end of day)
        var input = new AvailabilitySlotsQueryInput(
            OwnerId: ownerId,
            OwnerType: ownerType,
            FromUtc: oneOffDate.Date,
            ToUtc: oneOffDate.Date.AddDays(1).AddTicks(-1),
            SlotDuration: TimeSpan.FromMinutes(30)
        );

        // Act
        var result = await _query.ExecuteAsync(input);

        // Assert
        Assert.True(result.IsSuccess);
        var slots = result.Value;

        // Expected slots: 10:00-10:30, 10:30-11:00
        Assert.Equal(2, slots.Count);
        Assert.Equal(oneOffDate.Date.AddHours(10), slots[0].StartUtc);
        Assert.Equal(oneOffDate.Date.AddHours(10.5), slots[0].EndUtc);
        Assert.Equal(oneOffDate.Date.AddHours(10.5), slots[1].StartUtc);
        Assert.Equal(oneOffDate.Date.AddHours(11), slots[1].EndUtc);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldNotGenerateSlots_FromExclusionRules()
    {
        // Scenario: Verify that exclusion rules do *not* generate availability slots.
        // Note: This query calculates potential slots from availability rules. It does *not*
        // filter out slots that might fall within an exclusion rule. That filtering is expected
        // to happen either in the caller (e.g., GetReviewerAvailabilitySlotsQuery) or a higher-level availability calculation service.
        // This test confirms the query's *specific* behavior of only generating slots from !IsExcluded rules.
        // Arrange
        var today = FakeTimeProvider.GetUtcNow().UtcDateTime.Date;
        var dateToTest = today.AddDays(2); // A date in the future
        var ownerId = _testOwner.Id;
        var ownerType = AvailabilityOwnerType.Reviewer;

        // Add an exclusion rule for this date
        var exclusionRule = new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Owner = _testOwner,
            OwnerType = ownerType,
            DayOfWeek = dateToTest.DayOfWeek,
            StartTimeUtc = TimeSpan.Zero,
            EndTimeUtc = TimeSpan.Zero, // Full day exclusion
            ActiveFromUtc = dateToTest,
            ActiveUntilUtc = dateToTest,
            IsExcluded = true, // Exclusion
        };
        // Add an availability rule for the same day/time (this scenario might be unlikely in practice,
        // but it specifically tests if IsRuleActiveOnDate correctly filters exclusions)
        var availabilityRule = new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Owner = _testOwner,
            OwnerType = ownerType,
            DayOfWeek = dateToTest.DayOfWeek,
            StartTimeUtc = TimeSpan.FromHours(9),
            EndTimeUtc = TimeSpan.FromHours(10),
            ActiveFromUtc = dateToTest,
            ActiveUntilUtc = dateToTest,
            IsExcluded = false, // Availability
        };

        await AvailabilityRulesRepository.AddRangeAsync(new[] { exclusionRule, availabilityRule });
        await AvailabilityRulesRepository.SaveChangesAsync();

        // Query for that specific date (from midnight to end of day)
        var input = new AvailabilitySlotsQueryInput(
            OwnerId: ownerId,
            OwnerType: ownerType,
            FromUtc: dateToTest.Date,
            ToUtc: dateToTest.Date.AddDays(1).AddTicks(-1),
            SlotDuration: TimeSpan.FromMinutes(30)
        );

        // Act
        var result = await _query.ExecuteAsync(input);

        // Assert
        Assert.True(result.IsSuccess);
        var slots = result.Value;

        // Expected: Slots only generated from the AVAILABILITY rule, NOT the exclusion rule.
        // The exclusion rule is fetched but ignored by IsRuleActiveOnDate.
        // The *missing* logic is to then filter these slots against the exclusion rule.
        Assert.Equal(2, slots.Count); // Slots from 9:00-10:00 availability rule
        Assert.Equal(dateToTest.Date.AddHours(9), slots[0].StartUtc);
        Assert.Equal(dateToTest.Date.AddHours(9.5), slots[1].StartUtc);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldGenerateCorrectSlots_WhenSlotDurationDoesNotDivideEvenlyIntoRuleTime()
    {
        // Scenario: Test slot generation when the slot duration doesn't perfectly fit the rule's time range.
        // Arrange
        var today = FakeTimeProvider.GetUtcNow().UtcDateTime.Date;
        var dateToTest = today.AddDays(3); // A date in the future
        var ownerId = _testOwner.Id;
        var ownerType = AvailabilityOwnerType.Tutor;

        // Rule from 9:00 to 10:45 (1 hour 45 minutes)
        var rule = new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Owner = _testOwner,
            OwnerType = ownerType,
            DayOfWeek = dateToTest.DayOfWeek,
            StartTimeUtc = TimeSpan.FromHours(9),
            EndTimeUtc = TimeSpan.FromHours(10.75), // 10:45 AM
            ActiveFromUtc = today,
            ActiveUntilUtc = null,
            IsExcluded = false,
        };
        await AvailabilityRulesRepository.AddAsync(rule);
        await AvailabilityRulesRepository.SaveChangesAsync();

        // Query for that specific date with 30-minute slots (from midnight to end of day)
        var input = new AvailabilitySlotsQueryInput(
            OwnerId: ownerId,
            OwnerType: ownerType,
            FromUtc: dateToTest.Date,
            ToUtc: dateToTest.Date.AddDays(1).AddTicks(-1),
            SlotDuration: TimeSpan.FromMinutes(30)
        );

        // Act
        var result = await _query.ExecuteAsync(input);

        // Assert
        Assert.True(result.IsSuccess);
        var slots = result.Value;

        // Expected slots (30 mins):
        // 9:00-9:30 (End 9:30 <= 10:45) -> Generated
        // 9:30-10:00 (End 10:00 <= 10:45) -> Generated
        // 10:00-10:30 (End 10:30 <= 10:45) -> Generated
        // 10:30-11:00 (End 11:00 is NOT <= 10:45) -> NOT Generated
        Assert.Equal(3, slots.Count);
        Assert.Equal(dateToTest.Date.AddHours(9), slots[0].StartUtc);
        Assert.Equal(dateToTest.Date.AddHours(9.5), slots[1].StartUtc);
        Assert.Equal(dateToTest.Date.AddHours(10), slots[2].StartUtc);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldGenerateSlots_IgnoringInputFromUtcTimeComponentWhenStartingLoop()
    {
        // Scenario: Query has a FromUtc with a time component *after* the rule starts.
        // The query's loop starts from FromUtc.Date, not FromUtc itself.
        // This test confirms the current query behavior where slots starting before input.FromUtc time *can* be generated by *this* query.
        // Arrange
        var today = FakeTimeProvider.GetUtcNow().UtcDateTime.Date;
        var dateToTest = today.AddDays(4); // A date in the future
        var ownerId = _testOwner.Id;
        var ownerType = AvailabilityOwnerType.Reviewer;

        // Rule from 9:00 to 10:00
        var rule = new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Owner = _testOwner,
            OwnerType = ownerType,
            DayOfWeek = dateToTest.DayOfWeek,
            StartTimeUtc = TimeSpan.FromHours(9),
            EndTimeUtc = TimeSpan.FromHours(10),
            ActiveFromUtc = today,
            ActiveUntilUtc = null,
            IsExcluded = false,
        };
        await AvailabilityRulesRepository.AddAsync(rule);
        await AvailabilityRulesRepository.SaveChangesAsync();

        // Query starting on dateToTest, but from 9:30 AM
        var input = new AvailabilitySlotsQueryInput(
            OwnerId: ownerId,
            OwnerType: ownerType,
            FromUtc: dateToTest.Date.AddHours(9.5), // Query FROM 9:30 AM
            ToUtc: dateToTest.Date.AddDays(1).AddTicks(-1), // End of the day as DateTime
            SlotDuration: TimeSpan.FromMinutes(30)
        );

        // Act
        var result = await _query.ExecuteAsync(input);

        // Assert
        Assert.True(result.IsSuccess);
        var slots = result.Value;

        // Expected slots generated by *this query*: 9:00-9:30, 9:30-10:00
        // The loop starts from dateToTest (midnight), finds the rule 9-10, and generates slots from rule.StartTimeUtc (9:00).
        // A higher layer (like GetReviewerAvailabilitySlotsQuery) is responsible for filtering out slots <= current time or <= input.FromUtc time.
        Assert.Equal(2, slots.Count);
        Assert.Equal(dateToTest.Date.AddHours(9), slots[0].StartUtc);
        Assert.Equal(dateToTest.Date.AddHours(9.5), slots[1].StartUtc);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnEmptyList_WhenNoRulesExistForOwner()
    {
        // Scenario: Query for slots for a person who has no availability rules configured.
        // Arrange
        // No rules added for _testOwner explicitly in this test

        var today = FakeTimeProvider.GetUtcNow().UtcDateTime.Date;
        var input = new AvailabilitySlotsQueryInput(
            OwnerId: _testOwner.Id,
            OwnerType: AvailabilityOwnerType.Reviewer,
            FromUtc: today.Date,
            ToUtc: today.AddDays(7).Date.AddTicks(-1),
            SlotDuration: TimeSpan.FromMinutes(30)
        );

        // Act
        var result = await _query.ExecuteAsync(input);

        // Assert
        Assert.True(result.IsSuccess);
        var slots = result.Value;

        // Expected: Empty list
        Assert.Empty(slots);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnEmptyList_WhenRulesExistButAreOutsideQueryDateRange()
    {
        // Scenario: Rules exist for the owner, but the query date range does not overlap with any active rules.
        // Arrange
        var startDate = FakeTimeProvider.GetUtcNow().UtcDateTime.Date.AddYears(1); // Rules are next year
        var ownerId = _testOwner.Id;
        var ownerType = AvailabilityOwnerType.Tutor;

        var futureRule = new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Owner = _testOwner,
            OwnerType = ownerType,
            DayOfWeek = DayOfWeek.Monday,
            StartTimeUtc = TimeSpan.FromHours(9),
            EndTimeUtc = TimeSpan.FromHours(10),
            ActiveFromUtc = startDate,
            ActiveUntilUtc = null,
            IsExcluded = false,
        };
        await AvailabilityRulesRepository.AddAsync(futureRule);
        await AvailabilityRulesRepository.SaveChangesAsync();

        // Query for dates this year
        var queryFromDate = FakeTimeProvider.GetUtcNow().UtcDateTime.Date;
        var queryToDate = queryFromDate.AddDays(7);

        var input = new AvailabilitySlotsQueryInput(
            OwnerId: ownerId,
            OwnerType: ownerType,
            FromUtc: queryFromDate.Date,
            ToUtc: queryToDate.Date.AddDays(1).AddTicks(-1),
            SlotDuration: TimeSpan.FromMinutes(30)
        );

        // Act
        var result = await _query.ExecuteAsync(input);

        // Assert
        Assert.True(result.IsSuccess);
        var slots = result.Value;

        // Expected: Empty list, as no rules are active in the query date range
        Assert.Empty(slots);
    }
}
