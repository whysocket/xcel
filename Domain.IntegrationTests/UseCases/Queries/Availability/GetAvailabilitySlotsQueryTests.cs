using Application.UseCases.Queries.Availability;
using Domain.Entities;
using Xcel.TestUtils;

namespace Domain.IntegrationTests.UseCases.Queries.Availability;

// Integration tests for GetAvailabilitySlotsQuery
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
            RuleType = AvailabilityRuleType.AvailabilityStandard, // Use RuleType
            DayOfWeek = DayOfWeek.Monday,
            StartTimeUtc = TimeSpan.FromHours(9), // 9:00 AM
            EndTimeUtc = TimeSpan.FromHours(12), // 12:00 PM
            ActiveFromUtc = startDate,
            ActiveUntilUtc = null,
            // IsExcluded is removed
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
            RuleType = AvailabilityRuleType.AvailabilityStandard, // Use RuleType
            DayOfWeek = DayOfWeek.Tuesday,
            StartTimeUtc = TimeSpan.FromHours(9),
            EndTimeUtc = TimeSpan.FromHours(10), // 9:00-10:00
            ActiveFromUtc = startDate,
            ActiveUntilUtc = null,
            // IsExcluded is removed
        };
        var tuesdayAfternoon = new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Owner = _testOwner,
            OwnerType = ownerType,
            RuleType = AvailabilityRuleType.AvailabilityStandard, // Use RuleType
            DayOfWeek = DayOfWeek.Tuesday,
            StartTimeUtc = TimeSpan.FromHours(11),
            EndTimeUtc = TimeSpan.FromHours(12), // 11:00-12:00
            ActiveFromUtc = startDate,
            ActiveUntilUtc = null,
            // IsExcluded is removed
        };
        await AvailabilityRulesRepository.AddRangeAsync([tuesdayMorning, tuesdayAfternoon]);
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
            RuleType = AvailabilityRuleType.AvailabilityStandard, // Use RuleType
            DayOfWeek = DayOfWeek.Wednesday,
            StartTimeUtc = TimeSpan.FromHours(9),
            EndTimeUtc = TimeSpan.FromHours(10),
            ActiveFromUtc = activeFromDate,
            ActiveUntilUtc = null,
            // IsExcluded is removed
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
            RuleType = AvailabilityRuleType.AvailabilityStandard, // Use RuleType
            DayOfWeek = DayOfWeek.Thursday,
            StartTimeUtc = TimeSpan.FromHours(14),
            EndTimeUtc = TimeSpan.FromHours(15),
            ActiveFromUtc = today,
            ActiveUntilUtc = activeUntilDate, // Rule expires
            // IsExcluded is removed
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
            RuleType = AvailabilityRuleType.AvailabilityStandard, // Use RuleType
            DayOfWeek = DayOfWeek.Monday,
            StartTimeUtc = TimeSpan.FromHours(9),
            EndTimeUtc = TimeSpan.FromHours(10),
            ActiveFromUtc = startDate,
            ActiveUntilUtc = null,
            // IsExcluded is removed
        };
        var wednesdayRule = new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Owner = _testOwner,
            OwnerType = ownerType,
            RuleType = AvailabilityRuleType.AvailabilityStandard, // Use RuleType
            DayOfWeek = DayOfWeek.Wednesday,
            StartTimeUtc = TimeSpan.FromHours(14),
            EndTimeUtc = TimeSpan.FromHours(15),
            ActiveFromUtc = startDate,
            ActiveUntilUtc = null,
            // IsExcluded is removed
        };
        await AvailabilityRulesRepository.AddRangeAsync([mondayRule, wednesdayRule]);
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
        // Find the actual Monday and Wednesday within the query range
        var actualMonday = queryFromDate;
        while (actualMonday.DayOfWeek != DayOfWeek.Monday) actualMonday = actualMonday.AddDays(1);
        var actualWednesday = queryFromDate;
         while (actualWednesday.DayOfWeek != DayOfWeek.Wednesday) actualWednesday = actualWednesday.AddDays(1);


        Assert.All(slots.Where(s => s.StartUtc.Date == actualMonday.Date), s => Assert.Equal(DayOfWeek.Monday, s.StartUtc.DayOfWeek));
        Assert.All(slots.Where(s => s.StartUtc.Date == actualWednesday.Date), s => Assert.Equal(DayOfWeek.Wednesday, s.StartUtc.DayOfWeek));
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
            RuleType = AvailabilityRuleType.AvailabilityOneOff, // Use RuleType
            DayOfWeek = oneOffDate.DayOfWeek, // DayOfWeek must match the one-off date's DayOfWeek
            StartTimeUtc = TimeSpan.FromHours(10), // 10:00 AM
            EndTimeUtc = TimeSpan.FromHours(11), // 11:00 AM
            ActiveFromUtc = oneOffDate,
            ActiveUntilUtc = oneOffDate, // One-off date
            // IsExcluded is removed
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
    public async Task ExecuteAsync_ShouldNotGenerateSlots_WhenFullDayExclusionExists()
    {
        // Scenario: Verify that a full-day exclusion prevents any slots from being generated on that day.
        // Arrange
        var today = FakeTimeProvider.GetUtcNow().UtcDateTime.Date;
        var dateToTest = today.AddDays(2); // A date in the future
        var ownerId = _testOwner.Id;
        var ownerType = AvailabilityOwnerType.Reviewer;

        // Add a full-day exclusion rule for this date
        var fullDayExclusion = new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Owner = _testOwner,
            OwnerType = ownerType,
            RuleType = AvailabilityRuleType.ExclusionFullDay, // Use RuleType
            DayOfWeek = dateToTest.DayOfWeek,
            StartTimeUtc = TimeSpan.Zero,
            EndTimeUtc = TimeSpan.FromDays(1), // Full day exclusion
            ActiveFromUtc = dateToTest,
            ActiveUntilUtc = dateToTest,
            // IsExcluded is removed
        };
        // Add an availability rule for the same day (this should be ignored due to the full-day exclusion)
        var availabilityRule = new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Owner = _testOwner,
            OwnerType = ownerType,
            RuleType = AvailabilityRuleType.AvailabilityStandard, // Use RuleType
            DayOfWeek = dateToTest.DayOfWeek,
            StartTimeUtc = TimeSpan.FromHours(9),
            EndTimeUtc = TimeSpan.FromHours(10),
            ActiveFromUtc = dateToTest,
            ActiveUntilUtc = dateToTest,
            // IsExcluded is removed
        };

        await AvailabilityRulesRepository.AddRangeAsync([fullDayExclusion, availabilityRule]);
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

        // Expected: No slots should be generated because of the full-day exclusion.
        Assert.Empty(slots);
    }

     [Fact]
    public async Task ExecuteAsync_ShouldGenerateSlots_SubtractingTimeBasedExclusion()
    {
        // Scenario: Query for slots on a day with availability and a time-based exclusion that cuts into it.
        // Arrange
        var today = FakeTimeProvider.GetUtcNow().UtcDateTime.Date;
        var dateToTest = today.AddDays(4); // A date in the future
        var ownerId = _testOwner.Id;
        var ownerType = AvailabilityOwnerType.Tutor;

        // Add a broad availability rule
        var availabilityRule = new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Owner = _testOwner,
            OwnerType = ownerType,
            RuleType = AvailabilityRuleType.AvailabilityStandard, // Use RuleType
            DayOfWeek = dateToTest.DayOfWeek,
            StartTimeUtc = TimeSpan.FromHours(9), // 9:00 AM
            EndTimeUtc = TimeSpan.FromHours(17), // 5:00 PM
            ActiveFromUtc = dateToTest,
            ActiveUntilUtc = dateToTest,
            // IsExcluded is removed
        };
        // Add a time-based exclusion rule that cuts into the availability
        var timeBasedExclusion = new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Owner = _testOwner,
            OwnerType = ownerType,
            RuleType = AvailabilityRuleType.ExclusionTimeBased, // Use RuleType
            DayOfWeek = dateToTest.DayOfWeek,
            StartTimeUtc = TimeSpan.FromHours(12), // 12:00 PM
            EndTimeUtc = TimeSpan.FromHours(13), // 1:00 PM
            ActiveFromUtc = dateToTest,
            ActiveUntilUtc = dateToTest,
            // IsExcluded is removed
        };

        await AvailabilityRulesRepository.AddRangeAsync([availabilityRule, timeBasedExclusion]);
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

        // Expected slots:
        // From availability (9:00-17:00): 9:00, 9:30, 10:00, 10:30, 11:00, 11:30, 12:00, 12:30, 13:00, 13:30, 14:00, 14:30, 15:00, 15:30, 16:00, 16:30
        // Exclusion (12:00-13:00) removes slots starting at 12:00 and 12:30.
        // Net bookable intervals: 9:00-12:00 and 13:00-17:00
        // Slots in net intervals: 9:00, 9:30, 10:00, 10:30, 11:00, 11:30 (6 slots)
        //                        13:00, 13:30, 14:00, 14:30, 15:00, 15:30, 16:00, 16:30 (8 slots)
        // Total expected slots: 6 + 8 = 14
        Assert.Equal(14, slots.Count);

        var slotStartTimes = slots.Select(s => s.StartUtc.TimeOfDay).OrderBy(t => t).ToList();

        // Verify slots before exclusion are present
        Assert.Contains(TimeSpan.FromHours(9), slotStartTimes);
        Assert.Contains(TimeSpan.FromHours(11.5), slotStartTimes);

        // Verify slots within exclusion are NOT present
        Assert.DoesNotContain(TimeSpan.FromHours(12), slotStartTimes);
        Assert.DoesNotContain(TimeSpan.FromHours(12.5), slotStartTimes);

        // Verify slots after exclusion are present
        Assert.Contains(TimeSpan.FromHours(13), slotStartTimes);
        Assert.Contains(TimeSpan.FromHours(16.5), slotStartTimes);
    }

     [Fact]
    public async Task ExecuteAsync_ShouldGenerateSlots_WithMultipleAvailabilityBlocksAndExclusions()
    {
        // Scenario: Query for slots on a day with multiple availability blocks and multiple time-based exclusions.
        // Arrange
        var today = FakeTimeProvider.GetUtcNow().UtcDateTime.Date;
        var dateToTest = today.AddDays(5); // A date in the future
        var ownerId = _testOwner.Id;
        var ownerType = AvailabilityOwnerType.Reviewer;

        // Add multiple availability rules for the same day
        var morningAvailability = new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Owner = _testOwner,
            OwnerType = ownerType,
            RuleType = AvailabilityRuleType.AvailabilityStandard,
            DayOfWeek = dateToTest.DayOfWeek,
            StartTimeUtc = TimeSpan.FromHours(9),  // 9:00 AM
            EndTimeUtc = TimeSpan.FromHours(11), // 11:00 AM
            ActiveFromUtc = dateToTest,
            ActiveUntilUtc = dateToTest,
        };
        var afternoonAvailability = new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Owner = _testOwner,
            OwnerType = ownerType,
            RuleType = AvailabilityRuleType.AvailabilityStandard,
            DayOfWeek = dateToTest.DayOfWeek,
            StartTimeUtc = TimeSpan.FromHours(14), // 2:00 PM
            EndTimeUtc = TimeSpan.FromHours(17), // 5:00 PM
            ActiveFromUtc = dateToTest,
            ActiveUntilUtc = dateToTest,
        };

        // Add multiple time-based exclusion rules for the same day
        var earlyExclusion = new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Owner = _testOwner,
            OwnerType = AvailabilityOwnerType.Reviewer,
            RuleType = AvailabilityRuleType.ExclusionTimeBased,
            DayOfWeek = dateToTest.DayOfWeek,
            StartTimeUtc = TimeSpan.FromHours(9.5), // 9:30 AM
            EndTimeUtc = TimeSpan.FromHours(10), // 10:00 AM
            ActiveFromUtc = dateToTest,
            ActiveUntilUtc = dateToTest,
        };
         var lateExclusion = new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Owner = _testOwner,
            OwnerType = AvailabilityOwnerType.Reviewer,
            RuleType = AvailabilityRuleType.ExclusionTimeBased,
            DayOfWeek = dateToTest.DayOfWeek,
            StartTimeUtc = TimeSpan.FromHours(15), // 3:00 PM
            EndTimeUtc = TimeSpan.FromHours(15.5), // 3:30 PM
            ActiveFromUtc = dateToTest,
            ActiveUntilUtc = dateToTest,
        };


        await AvailabilityRulesRepository.AddRangeAsync([morningAvailability, afternoonAvailability, earlyExclusion, lateExclusion]);
        await AvailabilityRulesRepository.SaveChangesAsync();

        // Query for that specific date with 30-minute slots
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

        // Expected slots:
        // Morning Availability (9:00-11:00)
        // Exclusion (9:30-10:00) removes slot starting at 9:30
        // Net Morning: 9:00-9:30 and 10:00-11:00 -> Slots: 9:00, 10:00, 10:30 (3 slots)

        // Afternoon Availability (14:00-17:00)
        // Exclusion (15:00-15:30) removes slot starting at 15:00
        // Net Afternoon: 14:00-15:00 and 15:30-17:00 -> Slots: 14:00, 14:30, 15:30, 16:00, 16:30 (5 slots)

        // Total expected slots: 3 + 5 = 8
        Assert.Equal(8, slots.Count);

        var slotStartTimes = slots.Select(s => s.StartUtc.TimeOfDay).OrderBy(t => t).ToList();

        // Verify morning slots
        Assert.Contains(TimeSpan.FromHours(9), slotStartTimes);
        Assert.DoesNotContain(TimeSpan.FromHours(9.5), slotStartTimes); // Excluded
        Assert.Contains(TimeSpan.FromHours(10), slotStartTimes);
        Assert.Contains(TimeSpan.FromHours(10.5), slotStartTimes);

        // Verify afternoon slots
        Assert.Contains(TimeSpan.FromHours(14), slotStartTimes);
        Assert.Contains(TimeSpan.FromHours(14.5), slotStartTimes);
        Assert.DoesNotContain(TimeSpan.FromHours(15), slotStartTimes); // Excluded
        Assert.Contains(TimeSpan.FromHours(15.5), slotStartTimes);
        Assert.Contains(TimeSpan.FromHours(16), slotStartTimes);
        Assert.Contains(TimeSpan.FromHours(16.5), slotStartTimes);

        // Verify no slots outside these ranges
        Assert.DoesNotContain(TimeSpan.FromHours(11), slotStartTimes); // End of morning block
        Assert.DoesNotContain(TimeSpan.FromHours(11.5), slotStartTimes);
        Assert.DoesNotContain(TimeSpan.FromHours(12), slotStartTimes);
        Assert.DoesNotContain(TimeSpan.FromHours(12.5), slotStartTimes);
        Assert.DoesNotContain(TimeSpan.FromHours(13), slotStartTimes);
        Assert.DoesNotContain(TimeSpan.FromHours(13.5), slotStartTimes);
        Assert.DoesNotContain(TimeSpan.FromHours(17), slotStartTimes); // End of afternoon block
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
            RuleType = AvailabilityRuleType.AvailabilityStandard, // Use RuleType
            DayOfWeek = dateToTest.DayOfWeek,
            StartTimeUtc = TimeSpan.FromHours(9),
            EndTimeUtc = TimeSpan.FromHours(10.75), // 10:45 AM
            ActiveFromUtc = today,
            ActiveUntilUtc = null,
            // IsExcluded is removed
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
    public async Task ExecuteAsync_ShouldGenerateSlots_RespectingInputFromUtcTimeComponentWhenStartingLoop()
    {
        // Scenario: Query has a FromUtc with a time component *after* the rule starts.
        // The query's slot generation should start from the later of the interval start time and the input.FromUtc time.
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
            RuleType = AvailabilityRuleType.AvailabilityStandard, // Use RuleType
            DayOfWeek = dateToTest.DayOfWeek,
            StartTimeUtc = TimeSpan.FromHours(9),
            EndTimeUtc = TimeSpan.FromHours(10),
            ActiveFromUtc = today,
            ActiveUntilUtc = null,
            // IsExcluded is removed
        };
        await AvailabilityRulesRepository.AddAsync(rule);
        await AvailabilityRulesRepository.SaveChangesAsync();

        // Query starting on dateToTest, but from 9:30 AM
        var queryFromUtc = dateToTest.Date.AddHours(9.5); // Query FROM 9:30 AM

        var input = new AvailabilitySlotsQueryInput(
            OwnerId: ownerId,
            OwnerType: ownerType,
            FromUtc: queryFromUtc,
            ToUtc: dateToTest.Date.AddDays(1).AddTicks(-1), // End of the day as DateTime
            SlotDuration: TimeSpan.FromMinutes(30)
        );

        // Act
        var result = await _query.ExecuteAsync(input);

        // Assert
        Assert.True(result.IsSuccess);
        var slots = result.Value;

        // Expected slots generated by the query logic:
        // The net bookable interval is 9:00-10:00.
        // Slot generation starts from the later of interval start (9:00) and input.FromUtc (9:30), which is 9:30.
        // Slots: 9:30-10:00 (End 10:00 <= 10:00) -> Generated
        // 10:00-10:30 (End 10:30 is NOT <= 10:00) -> NOT Generated
        Assert.Single(slots);
        Assert.Equal(dateToTest.Date.AddHours(9.5), slots[0].StartUtc);
        Assert.Equal(dateToTest.Date.AddHours(10), slots[0].EndUtc);
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
            RuleType = AvailabilityRuleType.AvailabilityStandard, // Use RuleType
            DayOfWeek = DayOfWeek.Monday,
            StartTimeUtc = TimeSpan.FromHours(9),
            EndTimeUtc = TimeSpan.FromHours(10),
            ActiveFromUtc = startDate,
            ActiveUntilUtc = null,
            // IsExcluded is removed
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