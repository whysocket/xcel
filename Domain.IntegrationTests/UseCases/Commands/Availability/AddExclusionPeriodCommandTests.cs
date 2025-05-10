using Application.UseCases.Commands.Availability;
using Domain.Entities;
using Xcel.TestUtils; // Assuming Xcel.TestUtils namespace

namespace Domain.IntegrationTests.UseCases.Commands.Availability;

// Integration tests for AddExclusionPeriodCommand
public class AddExclusionPeriodCommandTests : BaseTest
{
    private IAddExclusionPeriodCommand _command = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _command = new AddExclusionPeriodCommand(
            AvailabilityRulesRepository, // AvailabilityRulesRepository available from BaseTest
            PersonsRepository, // PersonsRepository available from BaseTest
            CreateLogger<AddExclusionPeriodCommand>() // CreateLogger available from BaseTest
        );
    }

    [Fact]
    public async Task ExecuteAsync_ShouldAddFullDayExclusions_WhenDatesAreValid_ThreeDays()
    {
        // Scenario: Basic success case adding a range of full days exclusion
        // Arrange
        var person = new Person
        {
            FirstName = "Blocked",
            LastName = "ThreeDays",
            EmailAddress = "blocked3@xcel.com",
        };
        await PersonsRepository.AddAsync(person); // PersonsRepository available from BaseTest
        await PersonsRepository.SaveChangesAsync(); // SaveChangesAsync available from BaseTest

        var from = FakeTimeProvider.GetUtcNow().UtcDateTime.Date; // FakeTimeProvider available from BaseTest
        var to = from.AddDays(2); // Three days: from, from+1, from+2

        // Use the updated ExclusionPeriodInput with Type = FullDay
        var input = new ExclusionPeriodInput(
            person.Id,
            AvailabilityOwnerType.Reviewer,
            from,
            to,
            ExclusionType.FullDay
        );

        // Act
        var result = await _command.ExecuteAsync(input);

        // Assert
        Assert.True(result.IsSuccess);

        // Verify 3 rules were added for the specific dates
        // GetByOwnerAndDateRangeAsync fetches all rule types whose date range overlaps
        var rules = await AvailabilityRulesRepository.GetByOwnerAndDateRangeAsync( // AvailabilityRulesRepository available from BaseTest
            person.Id,
            AvailabilityOwnerType.Reviewer,
            from,
            to
        );
        Assert.Equal(3, rules.Count);
        Assert.All(
            rules,
            r =>
            {
                Assert.Equal(AvailabilityRuleType.ExclusionFullDay, r.RuleType); // Check RuleType is FullDay
                Assert.Equal(TimeSpan.Zero, r.StartTimeUtc); // StartTime should be Zero for FullDay
                Assert.Equal(TimeSpan.FromDays(1), r.EndTimeUtc); // EndTime should be TimeSpan.FromDays(1) for FullDay
                // Check ActiveFrom/Until are the same date within the range
                Assert.Equal(r.ActiveFromUtc.Date, r.ActiveUntilUtc!.Value.Date); // Compare Date parts
                Assert.True(r.ActiveFromUtc.Date >= from && r.ActiveFromUtc.Date <= to); // Compare Date parts
                Assert.Equal(r.ActiveFromUtc.DayOfWeek, r.DayOfWeek); // DayOfWeek matches the specific date
            }
        );

        // Verify rules were added for the correct specific dates
        var ruleDates = rules.Select(r => r.ActiveFromUtc.Date).OrderBy(d => d).ToList(); // Select Date parts
        Assert.Equal(from, ruleDates[0]);
        Assert.Equal(from.AddDays(1), ruleDates[1]);
        Assert.Equal(from.AddDays(2), ruleDates[2]);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldAddFullDayExclusions_WhenDatesAreValid_SingleDay()
    {
        // Scenario: Add a full day exclusion for just one day (Start and End dates are the same)
        // Arrange
        var person = new Person
        {
            FirstName = "Blocked",
            LastName = "OneDay",
            EmailAddress = "blocked1@xcel.com",
        };
        await PersonsRepository.AddAsync(person);
        await PersonsRepository.SaveChangesAsync();

        var date = FakeTimeProvider.GetUtcNow().UtcDateTime.Date;

        // Use the updated ExclusionPeriodInput with Type = FullDay
        var input = new ExclusionPeriodInput(
            person.Id,
            AvailabilityOwnerType.Tutor,
            date,
            date,
            ExclusionType.FullDay // Specify FullDay exclusion type
        );

        // Act
        var result = await _command.ExecuteAsync(input);

        // Assert
        Assert.True(result.IsSuccess);

        // Verify only 1 rule was added for that specific date
        var rules = await AvailabilityRulesRepository.GetByOwnerAndDateRangeAsync(
            person.Id,
            AvailabilityOwnerType.Tutor,
            date,
            date
        );
        Assert.Single(rules);
        var addedRule = rules.Single();

        Assert.Equal(AvailabilityRuleType.ExclusionFullDay, addedRule.RuleType); // Check RuleType is FullDay
        Assert.Equal(TimeSpan.Zero, addedRule.StartTimeUtc); // StartTime should be Zero for FullDay
        Assert.Equal(TimeSpan.FromDays(1), addedRule.EndTimeUtc); // EndTime should be TimeSpan.FromDays(1) for FullDay
        Assert.Equal(date, addedRule.ActiveFromUtc.Date); // Compare Date parts
        Assert.Equal(date, addedRule.ActiveUntilUtc!.Value.Date); // Compare Date parts
        Assert.Equal(date.DayOfWeek, addedRule.DayOfWeek);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldAddFullDayExclusions_IgnoringTimeComponentInInputDates()
    {
        // Scenario: Verify that the time component of the input DateTime is ignored,
        // and FullDay rules are created for the full dates.
        // Arrange
        var person = new Person
        {
            FirstName = "Time",
            LastName = "Ignored",
            EmailAddress = "timeignored@xcel.com",
        };
        await PersonsRepository.AddAsync(person);
        await PersonsRepository.SaveChangesAsync();

        var fromWithTime = FakeTimeProvider.GetUtcNow().UtcDateTime.Date.AddHours(9).AddMinutes(30); // Date + Time
        var toWithTime = fromWithTime.AddDays(2).AddHours(14).AddMinutes(45); // Another Date + Time

        var expectedFromDate = fromWithTime.Date;
        var expectedToDate = toWithTime.Date;

        // Use the updated ExclusionPeriodInput with Type = FullDay
        var input = new ExclusionPeriodInput(
            person.Id,
            AvailabilityOwnerType.Reviewer,
            fromWithTime,
            toWithTime,
            ExclusionType.FullDay // Specify FullDay exclusion type
        );

        // Act
        var result = await _command.ExecuteAsync(input);

        // Assert
        Assert.True(result.IsSuccess);

        // Verify rules were added using only the DATE parts of the input
        var rules = await AvailabilityRulesRepository.GetByOwnerAndDateRangeAsync(
            person.Id,
            AvailabilityOwnerType.Reviewer,
            expectedFromDate, // Query using just the date
            expectedToDate
        );
        var expectedRuleCount = (expectedToDate - expectedFromDate).Days + 1;
        Assert.Equal(expectedRuleCount, rules.Count);

        Assert.All(
            rules,
            r =>
            {
                Assert.Equal(AvailabilityRuleType.ExclusionFullDay, r.RuleType); // Check RuleType is FullDay
                Assert.Equal(TimeSpan.Zero, r.StartTimeUtc); // Time should be zero
                Assert.Equal(TimeSpan.FromDays(1), r.EndTimeUtc); // Time should be TimeSpan.FromDays(1)
                // ActiveFrom/Until should be the specific date iterated over, derived from the input dates
                Assert.Equal(r.ActiveFromUtc.Date, r.ActiveUntilUtc!.Value.Date); // Compare Date parts
                Assert.True(
                    r.ActiveFromUtc.Date >= expectedFromDate && r.ActiveFromUtc.Date <= expectedToDate // Compare Date parts
                );
                Assert.Equal(r.ActiveFromUtc.DayOfWeek, r.DayOfWeek);
            }
        );
    }

     [Fact]
    public async Task ExecuteAsync_ShouldAddSpecificTimeExclusions_WhenDatesAndTimeAreValid_SingleDay()
    {
        // Scenario: Add a specific time exclusion for a single day.
        // Arrange
        var person = new Person
        {
            FirstName = "Blocked",
            LastName = "SpecificTime",
            EmailAddress = "blockedtime@xcel.com",
        };
        await PersonsRepository.AddAsync(person);
        await PersonsRepository.SaveChangesAsync();

        var date = FakeTimeProvider.GetUtcNow().UtcDateTime.Date.AddDays(1); // A date in the future
        var startTime = TimeSpan.FromHours(10); // 10:00 AM
        var endTime = TimeSpan.FromHours(11.5); // 11:30 AM

        // Use the updated ExclusionPeriodInput with Type = SpecificTime
        var input = new ExclusionPeriodInput(
            person.Id,
            AvailabilityOwnerType.Tutor,
            date,
            date,
            ExclusionType.SpecificTime, // Specify SpecificTime exclusion type
            startTime,
            endTime
        );

        // Act
        var result = await _command.ExecuteAsync(input);

        // Assert
        Assert.True(result.IsSuccess);

        // Verify only 1 rule was added for that specific date
        var rules = await AvailabilityRulesRepository.GetByOwnerAndDateRangeAsync(
            person.Id,
            AvailabilityOwnerType.Tutor,
            date,
            date
        );
        Assert.Single(rules);
        var addedRule = rules.Single();

        Assert.Equal(AvailabilityRuleType.ExclusionTimeBased, addedRule.RuleType); // Check RuleType is TimeBased
        Assert.Equal(startTime, addedRule.StartTimeUtc); // Check StartTime
        Assert.Equal(endTime, addedRule.EndTimeUtc); // Check EndTime
        Assert.Equal(date, addedRule.ActiveFromUtc.Date); // Compare Date parts
        Assert.Equal(date, addedRule.ActiveUntilUtc!.Value.Date); // Compare Date parts
        Assert.Equal(date.DayOfWeek, addedRule.DayOfWeek);
    }

     [Fact]
    public async Task ExecuteAsync_ShouldAddSpecificTimeExclusions_WhenDatesAndTimeAreValid_MultipleDays()
    {
        // Scenario: Add a specific time exclusion over a range of days.
        // Arrange
        var person = new Person
        {
            FirstName = "Blocked",
            LastName = "SpecificTimeRange",
            EmailAddress = "blockedtimerange@xcel.com",
        };
        await PersonsRepository.AddAsync(person);
        await PersonsRepository.SaveChangesAsync();

        var from = FakeTimeProvider.GetUtcNow().UtcDateTime.Date.AddDays(5); // Future start date
        var to = from.AddDays(2); // Covers 3 days
        var startTime = TimeSpan.FromHours(14); // 2:00 PM
        var endTime = TimeSpan.FromHours(16); // 4:00 PM

        // Use the updated ExclusionPeriodInput with Type = SpecificTime
        var input = new ExclusionPeriodInput(
            person.Id,
            AvailabilityOwnerType.Reviewer,
            from,
            to,
            ExclusionType.SpecificTime, // Specify SpecificTime exclusion type
            startTime,
            endTime
        );

        // Act
        var result = await _command.ExecuteAsync(input);

        // Assert
        Assert.True(result.IsSuccess);

        // Verify 3 rules were added (one for each day in the range)
        var rules = await AvailabilityRulesRepository.GetByOwnerAndDateRangeAsync(
            person.Id,
            AvailabilityOwnerType.Reviewer,
            from,
            to
        );
        Assert.Equal(3, rules.Count);
        Assert.All(
            rules,
            r =>
            {
                Assert.Equal(AvailabilityRuleType.ExclusionTimeBased, r.RuleType); // Check RuleType is TimeBased
                Assert.Equal(startTime, r.StartTimeUtc); // Check StartTime
                Assert.Equal(endTime, r.EndTimeUtc); // Check EndTime
                // Check ActiveFrom/Until are the same date within the range
                Assert.Equal(r.ActiveFromUtc.Date, r.ActiveUntilUtc!.Value.Date); // Compare Date parts
                Assert.True(r.ActiveFromUtc.Date >= from && r.ActiveFromUtc.Date <= to); // Compare Date parts
                Assert.Equal(r.ActiveFromUtc.DayOfWeek, r.DayOfWeek); // DayOfWeek matches the specific date
            }
        );

        // Verify rules were added for the correct specific dates
        var ruleDates = rules.Select(r => r.ActiveFromUtc.Date).OrderBy(d => d).ToList(); // Select Date parts
        Assert.Equal(from, ruleDates[0]);
        Assert.Equal(from.AddDays(1), ruleDates[1]);
        Assert.Equal(from.AddDays(2), ruleDates[2]);
    }


    [Fact]
    public async Task ExecuteAsync_ShouldAddExclusions_OverlappingExistingAvailabilityRules()
    {
        // Scenario: Adding an exclusion period over days that already have specific availability slots.
        // The command should *still* add the exclusion rules; the override logic is handled when checking availability.
        // Arrange
        var person = new Person
        {
            FirstName = "Overlap",
            LastName = "Availability",
            EmailAddress = "overlapav@xcel.com",
        };
        await PersonsRepository.AddAsync(person);
        await PersonsRepository.SaveChangesAsync();

        var dateToBlock = FakeTimeProvider.GetUtcNow().UtcDateTime.Date.AddDays(5); // A date in the future
        var nextDay = dateToBlock.AddDays(1);

        // Add existing availability rules on these dates
        var existingAvailability1 = new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            OwnerId = person.Id,
            Owner = person,
            OwnerType = AvailabilityOwnerType.Reviewer,
            RuleType = AvailabilityRuleType.AvailabilityStandard, // Existing availability
            DayOfWeek = dateToBlock.DayOfWeek,
            StartTimeUtc = TimeSpan.FromHours(9),
            EndTimeUtc = TimeSpan.FromHours(10),
            ActiveFromUtc = dateToBlock,
            ActiveUntilUtc = dateToBlock,
        };
        var existingAvailability2 = new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            OwnerId = person.Id,
            Owner = person,
            OwnerType = AvailabilityOwnerType.Reviewer,
            RuleType = AvailabilityRuleType.AvailabilityStandard, // Existing availability on next day
            DayOfWeek = nextDay.DayOfWeek,
            StartTimeUtc = TimeSpan.FromHours(14),
            EndTimeUtc = TimeSpan.FromHours(16),
            ActiveFromUtc = nextDay,
            ActiveUntilUtc = nextDay,
        };
        await AvailabilityRulesRepository.AddRangeAsync(
            [existingAvailability1, existingAvailability2]
        );
        await AvailabilityRulesRepository.SaveChangesAsync();

        // Input: FullDay Exclusion period covering these two dates
        var input = new ExclusionPeriodInput(
            person.Id,
            AvailabilityOwnerType.Reviewer,
            dateToBlock,
            nextDay, // Covers two days
            ExclusionType.FullDay // Adding FullDay exclusion
        );

        // Act
        var result = await _command.ExecuteAsync(input);

        // Assert
        Assert.True(result.IsSuccess);

        // Should find both the original availability rules AND the two new exclusion rules
        var allRulesOnDates = await AvailabilityRulesRepository.GetByOwnerAndDateRangeAsync(
            person.Id,
            AvailabilityOwnerType.Reviewer,
            dateToBlock.AddDays(-1), // Query a slightly wider range just in case
            nextDay.AddDays(1)
        );
        Assert.Equal(4, allRulesOnDates.Count); // 2 existing availability + 2 new exclusion rules

        Assert.Contains(
            allRulesOnDates,
            r => r.RuleType == AvailabilityRuleType.ExclusionFullDay && r.ActiveFromUtc.Date == dateToBlock.Date && r.ActiveUntilUtc!.Value.Date == dateToBlock.Date // Check RuleType and Dates
        );
        Assert.Contains(
            allRulesOnDates,
            r => r.RuleType == AvailabilityRuleType.ExclusionFullDay && r.ActiveFromUtc.Date == nextDay.Date && r.ActiveUntilUtc!.Value.Date == nextDay.Date // Check RuleType and Dates
        );

        // Verify the original availability rules still exist with their correct RuleType
        Assert.Contains(allRulesOnDates, r => r.RuleType == AvailabilityRuleType.AvailabilityStandard && r.Id == existingAvailability1.Id);
        Assert.Contains(allRulesOnDates, r => r.RuleType == AvailabilityRuleType.AvailabilityStandard && r.Id == existingAvailability2.Id);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldAddExclusions_OverlappingExistingExclusionRules()
    {
        // Scenario: Adding an exclusion period over days that already have existing exclusion rules.
        // The command should *still* add the new exclusion rules, resulting in redundant rules.
        // Arrange
        var person = new Person
        {
            FirstName = "Overlap",
            LastName = "Exclusion",
            EmailAddress = "overlapex@xcel.com",
        };
        await PersonsRepository.AddAsync(person);
        await PersonsRepository.SaveChangesAsync();

        var startDate = FakeTimeProvider.GetUtcNow().UtcDateTime.Date.AddDays(10); // Future start date
        var endDate = startDate.AddDays(2); // Covers 3 days

        // Add existing full-day exclusion rules for some of these dates
        var existingExclusion1 = new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            OwnerId = person.Id,
            Owner = person,
            OwnerType = AvailabilityOwnerType.Reviewer,
            RuleType = AvailabilityRuleType.ExclusionFullDay, // Existing full-day exclusion
            DayOfWeek = startDate.DayOfWeek,
            StartTimeUtc = TimeSpan.Zero,
            EndTimeUtc = TimeSpan.FromDays(1),
            ActiveFromUtc = startDate,
            ActiveUntilUtc = startDate,
        };
        var existingExclusion2 = new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            OwnerId = person.Id,
            Owner = person,
            OwnerType = AvailabilityOwnerType.Reviewer,
             RuleType = AvailabilityRuleType.ExclusionFullDay, // Existing full-day exclusion
            DayOfWeek = startDate.AddDays(1).DayOfWeek,
            StartTimeUtc = TimeSpan.Zero,
            EndTimeUtc = TimeSpan.FromDays(1),
            ActiveFromUtc = startDate.AddDays(1),
            ActiveUntilUtc = startDate.AddDays(1),
        };
        await AvailabilityRulesRepository.AddRangeAsync(
            [existingExclusion1, existingExclusion2]
        );
        await AvailabilityRulesRepository.SaveChangesAsync();

        // Input: FullDay Exclusion period covering the full 3 days
        var input = new ExclusionPeriodInput(
            person.Id,
            AvailabilityOwnerType.Reviewer,
            startDate,
            endDate,
            ExclusionType.FullDay // Adding FullDay exclusion
        );

        // Act
        var result = await _command.ExecuteAsync(input);

        // Assert
        Assert.True(result.IsSuccess);

        // Verify the *new* exclusion rules were added, alongside the old ones
        var allRulesOnDates = await AvailabilityRulesRepository.GetByOwnerAndDateRangeAsync(
            person.Id,
            AvailabilityOwnerType.Reviewer,
            startDate,
            endDate
        );

        // Should find the 2 original exclusion rules AND the 3 new exclusion rules = 5 total
        Assert.Equal(5, allRulesOnDates.Count);
        Assert.All(allRulesOnDates, r => Assert.Equal(AvailabilityRuleType.ExclusionFullDay, r.RuleType)); // All rules on these dates are FullDay exclusions

        // Verify the specific dates covered (each date should have at least one rule, some two)
        var ruleDates = allRulesOnDates.Select(r => r.ActiveFromUtc.Date).ToList(); // Select Date parts
        Assert.Contains(startDate, ruleDates); // New rule for start date + original rule
        Assert.Contains(startDate.AddDays(1), ruleDates); // New rule for start+1 day + original rule
        Assert.Contains(endDate, ruleDates); // New rule for end date
    }

     [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenSpecificTimeRequiredButNotProvided()
    {
        // Scenario: Attempt to add a SpecificTime exclusion without providing StartTimeUtc/EndTimeUtc.
        // Arrange
        var person = new Person
        {
            FirstName = "Missing",
            LastName = "Time",
            EmailAddress = "missingtime@xcel.com",
        };
        await PersonsRepository.AddAsync(person);
        await PersonsRepository.SaveChangesAsync();

        var date = FakeTimeProvider.GetUtcNow().UtcDateTime.Date.AddDays(1);

        // Input: SpecificTime exclusion but times are null
        var input = new ExclusionPeriodInput(
            person.Id,
            AvailabilityOwnerType.Tutor,
            date,
            date,
            ExclusionType.SpecificTime, // Specify SpecificTime exclusion type
            null, // StartTimeUtc is null
            null // EndTimeUtc is null
        );

        // Act
        var result = await _command.ExecuteAsync(input);

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(AddExclusionPeriodCommandErrors.SpecificTimeRequired, error);

        // Verify no rules were added
        var rules = await AvailabilityRulesRepository.GetByOwnerAndDateRangeAsync(
            person.Id,
            AvailabilityOwnerType.Tutor,
            date,
            date
        );
        Assert.Empty(rules);
    }

     [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenSpecificTimeRangeIsInvalid()
    {
        // Scenario: Attempt to add a SpecificTime exclusion with an invalid time range (Start >= End).
        // Arrange
        var person = new Person
        {
            FirstName = "Invalid",
            LastName = "TimeRange",
            EmailAddress = "invalidtimerange@xcel.com",
        };
        await PersonsRepository.AddAsync(person);
        await PersonsRepository.SaveChangesAsync();

        var date = FakeTimeProvider.GetUtcNow().UtcDateTime.Date.AddDays(1);

        // Input: SpecificTime exclusion with invalid time range
        var input = new ExclusionPeriodInput(
            person.Id,
            AvailabilityOwnerType.Tutor,
            date,
            date,
            ExclusionType.SpecificTime, // Specify SpecificTime exclusion type
            TimeSpan.FromHours(11), // Start time
            TimeSpan.FromHours(10) // End time (Invalid: Start >= End)
        );

        // Act
        var result = await _command.ExecuteAsync(input);

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(AddExclusionPeriodCommandErrors.InvalidTimeRange, error);

        // Verify no rules were added
        var rules = await AvailabilityRulesRepository.GetByOwnerAndDateRangeAsync(
            person.Id,
            AvailabilityOwnerType.Tutor,
            date,
            date
        );
        Assert.Empty(rules);
    }


    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenPersonDoesNotExist()
    {
        // Scenario: Attempt to add exclusion for a person who does not exist.
        // Arrange
        var nonExistentPersonId = Guid.NewGuid();
        // Use the updated ExclusionPeriodInput with Type
        var input = new ExclusionPeriodInput(
            nonExistentPersonId,
            AvailabilityOwnerType.Tutor,
            FakeTimeProvider.GetUtcNow().UtcDateTime.Date,
            FakeTimeProvider.GetUtcNow().UtcDateTime.Date,
            ExclusionType.FullDay // Specify a type
        );

        // Act
        var result = await _command.ExecuteAsync(input);

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(AddExclusionPeriodCommandErrors.PersonNotFound(input.OwnerId), error);

        // Verify no rules were added
        var rules = await AvailabilityRulesRepository.GetByOwnerAndDateRangeAsync(
            nonExistentPersonId,
            AvailabilityOwnerType.Tutor,
            input.StartDateUtc.Date,
            input.EndDateUtc.Date
        );
        Assert.Empty(rules);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenStartDateAfterEndDate()
    {
        // Scenario: Attempt to add exclusion where StartDate is after EndDate.
        // Arrange
        var person = new Person
        {
            FirstName = "Invalid",
            LastName = "Range",
            EmailAddress = "invalid@xcel.com",
        };
        await PersonsRepository.AddAsync(person);
        await PersonsRepository.SaveChangesAsync();

        // Use the updated ExclusionPeriodInput with Type
        var input = new ExclusionPeriodInput(
            person.Id,
            AvailabilityOwnerType.Reviewer,
            FakeTimeProvider.GetUtcNow().UtcDateTime.Date.AddDays(2), // Start after End
            FakeTimeProvider.GetUtcNow().UtcDateTime.Date,
            ExclusionType.FullDay // Specify a type
        );

        // Act
        var result = await _command.ExecuteAsync(input);

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(AddExclusionPeriodCommandErrors.InvalidDateRange, error);

        // Verify no rules were added (due to validation fail)
        var rules = await AvailabilityRulesRepository.GetByOwnerAndDateRangeAsync(
            person.Id,
            AvailabilityOwnerType.Reviewer,
            input.EndDateUtc.Date, // Use EndDate as start for query range
            input.StartDateUtc.Date // Use StartDate as end for query range
        );
        Assert.Empty(rules);
    }
}
