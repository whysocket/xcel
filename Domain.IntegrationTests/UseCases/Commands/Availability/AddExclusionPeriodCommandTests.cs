using Application.UseCases.Commands.Availability;
using Domain.Entities;
using Xcel.TestUtils;

namespace Domain.IntegrationTests.UseCases.Commands.Availability;

public class AddExclusionPeriodCommandTests : BaseTest
{
    private IAddExclusionPeriodCommand _command = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _command = new AddExclusionPeriodCommand(
            AvailabilityRulesRepository,
            PersonsRepository,
            CreateLogger<AddExclusionPeriodCommand>()
        );
    }

    [Fact]
    public async Task ExecuteAsync_ShouldAddExclusions_WhenDatesAreValid_ThreeDays()
    {
        // Scenario: Basic success case adding a range of days
        // Arrange
        var person = new Person
        {
            FirstName = "Blocked",
            LastName = "ThreeDays",
            EmailAddress = "blocked3@xcel.com",
        };
        await PersonsRepository.AddAsync(person);
        await PersonsRepository.SaveChangesAsync();

        var from = FakeTimeProvider.GetUtcNow().UtcDateTime.Date;
        var to = from.AddDays(2); // Three days: from, from+1, from+2

        var input = new ExclusionPeriodInput(person.Id, AvailabilityOwnerType.Reviewer, from, to);

        // Act
        var result = await _command.ExecuteAsync(input);

        // Assert
        Assert.True(result.IsSuccess);

        // Verify 3 rules were added for the specific dates
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
                Assert.True(r.IsExcluded);
                Assert.Equal(TimeSpan.Zero, r.StartTimeUtc);
                Assert.Equal(TimeSpan.Zero, r.EndTimeUtc);
                // Check ActiveFrom/Until are the same date within the range
                Assert.Equal(r.ActiveFromUtc, r.ActiveUntilUtc);
                Assert.True(r.ActiveFromUtc >= from && r.ActiveFromUtc <= to);
                Assert.Equal(r.ActiveFromUtc.DayOfWeek, r.DayOfWeek); // DayOfWeek matches the specific date
            }
        );

        // Verify rules were added for the correct specific dates
        var ruleDates = rules.Select(r => r.ActiveFromUtc).OrderBy(d => d).ToList();
        Assert.Equal(from, ruleDates[0]);
        Assert.Equal(from.AddDays(1), ruleDates[1]);
        Assert.Equal(from.AddDays(2), ruleDates[2]);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldAddExclusions_WhenDatesAreValid_SingleDay()
    {
        // Scenario: Add an exclusion for just one day (Start and End dates are the same)
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

        var input = new ExclusionPeriodInput(person.Id, AvailabilityOwnerType.Tutor, date, date);

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

        Assert.True(addedRule.IsExcluded);
        Assert.Equal(TimeSpan.Zero, addedRule.StartTimeUtc);
        Assert.Equal(TimeSpan.Zero, addedRule.EndTimeUtc);
        Assert.Equal(date, addedRule.ActiveFromUtc);
        Assert.Equal(date, addedRule.ActiveUntilUtc);
        Assert.Equal(date.DayOfWeek, addedRule.DayOfWeek);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldAddExclusions_IgnoringTimeComponentInInputDates()
    {
        // Scenario: Verify that the time component of the input DateTime is ignored,
        // and rules are created for the full dates using TimeSpan.Zero.
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

        var input = new ExclusionPeriodInput(
            person.Id,
            AvailabilityOwnerType.Reviewer,
            fromWithTime,
            toWithTime
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
                Assert.True(r.IsExcluded);
                Assert.Equal(TimeSpan.Zero, r.StartTimeUtc); // Time should be zero
                Assert.Equal(TimeSpan.Zero, r.EndTimeUtc); // Time should be zero
                // ActiveFrom/Until should be the specific date iterated over, derived from the input dates
                Assert.Equal(r.ActiveFromUtc, r.ActiveUntilUtc);
                Assert.True(
                    r.ActiveFromUtc >= expectedFromDate && r.ActiveFromUtc <= expectedToDate
                );
                Assert.Equal(r.ActiveFromUtc.DayOfWeek, r.DayOfWeek);
            }
        );
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
            DayOfWeek = dateToBlock.DayOfWeek,
            StartTimeUtc = TimeSpan.FromHours(9),
            EndTimeUtc = TimeSpan.FromHours(10),
            ActiveFromUtc = dateToBlock,
            ActiveUntilUtc = dateToBlock,
            IsExcluded = false, // Existing availability
        };
        var existingAvailability2 = new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            OwnerId = person.Id,
            Owner = person,
            OwnerType = AvailabilityOwnerType.Reviewer,
            DayOfWeek = nextDay.DayOfWeek,
            StartTimeUtc = TimeSpan.FromHours(14),
            EndTimeUtc = TimeSpan.FromHours(16),
            ActiveFromUtc = nextDay,
            ActiveUntilUtc = nextDay,
            IsExcluded = false, // Existing availability on next day
        };
        await AvailabilityRulesRepository.AddRangeAsync(
            new[] { existingAvailability1, existingAvailability2 }
        );
        await AvailabilityRulesRepository.SaveChangesAsync();

        // Input: Exclusion period covering these two dates
        var input = new ExclusionPeriodInput(
            person.Id,
            AvailabilityOwnerType.Reviewer,
            dateToBlock,
            nextDay // Covers two days
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
            r => r.IsExcluded && r.ActiveFromUtc == dateToBlock && r.ActiveUntilUtc == dateToBlock
        );
        Assert.Contains(
            allRulesOnDates,
            r => r.IsExcluded && r.ActiveFromUtc == nextDay && r.ActiveUntilUtc == nextDay
        );

        // Verify the original availability rules still exist
        Assert.Contains(allRulesOnDates, r => !r.IsExcluded && r.Id == existingAvailability1.Id);
        Assert.Contains(allRulesOnDates, r => !r.IsExcluded && r.Id == existingAvailability2.Id);
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

        // Add existing exclusion rules for some of these dates
        var existingExclusion1 = new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            OwnerId = person.Id,
            Owner = person,
            OwnerType = AvailabilityOwnerType.Reviewer,
            DayOfWeek = startDate.DayOfWeek,
            StartTimeUtc = TimeSpan.Zero,
            EndTimeUtc = TimeSpan.Zero,
            ActiveFromUtc = startDate,
            ActiveUntilUtc = startDate,
            IsExcluded = true, // Existing exclusion for the first day
        };
        var existingExclusion2 = new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            OwnerId = person.Id,
            Owner = person,
            OwnerType = AvailabilityOwnerType.Reviewer,
            DayOfWeek = startDate.AddDays(1).DayOfWeek,
            StartTimeUtc = TimeSpan.Zero,
            EndTimeUtc = TimeSpan.Zero,
            ActiveFromUtc = startDate.AddDays(1),
            ActiveUntilUtc = startDate.AddDays(1),
            IsExcluded = true, // Existing exclusion for the second day
        };
        await AvailabilityRulesRepository.AddRangeAsync(
            new[] { existingExclusion1, existingExclusion2 }
        );
        await AvailabilityRulesRepository.SaveChangesAsync();

        // Input: Exclusion period covering the full 3 days
        var input = new ExclusionPeriodInput(
            person.Id,
            AvailabilityOwnerType.Reviewer,
            startDate,
            endDate
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

        // Should find the 2 original exclusion rules AND the 3 new exclusion rules
        Assert.Equal(5, allRulesOnDates.Count);
        Assert.All(allRulesOnDates, r => Assert.True(r.IsExcluded)); // All rules on these dates are exclusions

        // Verify the specific dates covered (each date should have at least one rule, some two)
        var ruleDates = allRulesOnDates.Select(r => r.ActiveFromUtc).ToList();
        Assert.Contains(startDate, ruleDates); // New rule for start date + original rule
        Assert.Contains(startDate.AddDays(1), ruleDates); // New rule for start+1 day + original rule
        Assert.Contains(endDate, ruleDates); // New rule for end date
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenPersonDoesNotExist()
    {
        // Scenario: Attempt to add exclusion for a person who does not exist.
        // Arrange
        var nonExistentPersonId = Guid.NewGuid();
        var input = new ExclusionPeriodInput(
            nonExistentPersonId,
            AvailabilityOwnerType.Tutor,
            FakeTimeProvider.GetUtcNow().UtcDateTime.Date,
            FakeTimeProvider.GetUtcNow().UtcDateTime.Date
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

        var input = new ExclusionPeriodInput(
            person.Id,
            AvailabilityOwnerType.Reviewer,
            FakeTimeProvider.GetUtcNow().UtcDateTime.Date.AddDays(2), // Start after End
            FakeTimeProvider.GetUtcNow().UtcDateTime.Date
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
