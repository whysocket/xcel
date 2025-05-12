using Application.UseCases.Commands.Availability;
using Domain.Entities;
using Xcel.TestUtils; // Assuming Xcel.TestUtils namespace

namespace Domain.IntegrationTests.UseCases.Commands.Availability;

// Integration tests for AddOneOffAvailabilitySlotCommand
public class AddOneOffAvailabilitySlotCommandTests : BaseTest
{
    private IAddOneOffAvailabilitySlotCommand _command = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _command = new AddOneOffAvailabilitySlotCommand(
            AvailabilityRulesRepository,
            PersonsRepository,
            CreateLogger<AddOneOffAvailabilitySlotCommand>()
        );
    }

    [Fact]
    public async Task ExecuteAsync_ShouldAddSlot_WhenNoOverlapExists()
    {
        // Scenario: Call AddOneOffAvailabilitySlotCommand with valid input when no overlapping rules exist.
        // Arrange
        var person = new Person
        {
            FirstName = "Available",
            LastName = "Once",
            EmailAddress = "available@xcel.com",
        };
        await PersonsRepository.AddAsync(person);
        await PersonsRepository.SaveChangesAsync();

        var date = FakeTimeProvider.GetUtcNow().UtcDateTime.Date;
        var start = date.AddHours(9); // 9:00 AM UTC
        var end = date.AddHours(10); // 10:00 AM UTC

        var input = new OneOffAvailabilityInput(person.Id, AvailabilityOwnerType.Tutor, start, end);

        // Act
        var result = await _command.ExecuteAsync(input);

        // Assert
        Assert.True(result.IsSuccess);

        // GetRulesActiveOnDateAsync fetches all rules active on the date regardless of type
        var rules = await AvailabilityRulesRepository.GetRulesActiveOnDateAsync(person.Id, date);
        Assert.Single(rules);
        var addedRule = rules.Single();

        // Verify the properties, including the correct RuleType
        Assert.Equal(AvailabilityRuleType.AvailabilityOneOff, addedRule.RuleType); // Check RuleType
        Assert.Equal(TimeSpan.FromHours(9), addedRule.StartTimeUtc);
        Assert.Equal(TimeSpan.FromHours(10), addedRule.EndTimeUtc);
        Assert.Equal(date, addedRule.ActiveFromUtc.Date); // Compare Date parts
        Assert.Equal(date, addedRule.ActiveUntilUtc!.Value.Date); // Compare Date parts
        Assert.Equal(person.Id, addedRule.OwnerId);
        Assert.Equal(AvailabilityOwnerType.Tutor, addedRule.OwnerType);
        Assert.Equal(date.DayOfWeek, addedRule.DayOfWeek);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenSlotOverlapsWithExistingAvailability()
    {
        // Scenario: Call AddOneOffAvailabilitySlotCommand with input that overlaps with an existing standard availability rule.
        // Arrange
        var person = new Person
        {
            FirstName = "Overlapping",
            LastName = "Slot",
            EmailAddress = "overlap@xcel.com",
        };
        await PersonsRepository.AddAsync(person);
        await PersonsRepository.SaveChangesAsync();

        var date = FakeTimeProvider.GetUtcNow().UtcDateTime.Date;

        // Add an existing standard availability rule from 9:00 to 10:00
        var existingRule = new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            OwnerId = person.Id,
            Owner = person,
            OwnerType = AvailabilityOwnerType.Tutor,
            RuleType = AvailabilityRuleType.AvailabilityStandard, // Existing standard rule
            DayOfWeek = date.DayOfWeek,
            StartTimeUtc = TimeSpan.FromHours(9),
            EndTimeUtc = TimeSpan.FromHours(10),
            ActiveFromUtc = date,
            ActiveUntilUtc = date,
        };
        await AvailabilityRulesRepository.AddAsync(existingRule);
        await AvailabilityRulesRepository.SaveChangesAsync();

        // Input slot from 9:30 to 10:30 (overlaps with existing rule)
        var inputStart = date.AddHours(9).AddMinutes(30);
        var inputEnd = date.AddHours(10).AddMinutes(30);
        var input = new OneOffAvailabilityInput(
            person.Id,
            AvailabilityOwnerType.Tutor,
            inputStart,
            inputEnd
        );

        // Act
        var result = await _command.ExecuteAsync(input);

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(AddOneOffAvailabilitySlotCommandErrors.OverlappingSlot, error);

        // Verify no new rule was added
        var rules = await AvailabilityRulesRepository.GetRulesActiveOnDateAsync(person.Id, date);
        Assert.Single(rules); // Should still only be the original rule
        Assert.Equal(existingRule.Id, rules.Single().Id); // Verify it's the original rule
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenSlotOverlapsWithExistingExclusion()
    {
        // Scenario: Call AddOneOffAvailabilitySlotCommand with input that overlaps with an existing time-based exclusion rule.
        // Arrange
        var person = new Person
        {
            FirstName = "Overlapping",
            LastName = "Exclusion",
            EmailAddress = "overlapexclusion@xcel.com",
        };
        await PersonsRepository.AddAsync(person);
        await PersonsRepository.SaveChangesAsync();

        var date = FakeTimeProvider.GetUtcNow().UtcDateTime.Date;

        // Add an existing time-based exclusion rule from 9:00 to 10:00
        var existingExclusionRule = new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            OwnerId = person.Id,
            Owner = person,
            OwnerType = AvailabilityOwnerType.Tutor,
            RuleType = AvailabilityRuleType.ExclusionTimeBased, // Existing time-based exclusion rule
            DayOfWeek = date.DayOfWeek,
            StartTimeUtc = TimeSpan.FromHours(9),
            EndTimeUtc = TimeSpan.FromHours(10),
            ActiveFromUtc = date,
            ActiveUntilUtc = date,
        };
        await AvailabilityRulesRepository.AddAsync(existingExclusionRule);
        await AvailabilityRulesRepository.SaveChangesAsync();

        // Input slot from 9:30 to 10:30 (overlaps with existing exclusion rule)
        var inputStart = date.AddHours(9).AddMinutes(30);
        var inputEnd = date.AddHours(10).AddMinutes(30);
        var input = new OneOffAvailabilityInput(
            person.Id,
            AvailabilityOwnerType.Tutor,
            inputStart,
            inputEnd
        );

        // Act
        var result = await _command.ExecuteAsync(input);

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(AddOneOffAvailabilitySlotCommandErrors.OverlappingSlot, error);

        // Verify no new rule was added
        var rules = await AvailabilityRulesRepository.GetRulesActiveOnDateAsync(person.Id, date);
        Assert.Single(rules); // Should still only be the original rule
        Assert.Equal(existingExclusionRule.Id, rules.Single().Id); // Verify it's the original rule
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenStartTimeAtOrAfterEndTime()
    {
        // Scenario: Call AddOneOffAvailabilitySlotCommand with an invalid time range (Start >= End).
        // Arrange
        var person = new Person
        {
            FirstName = "Invalid",
            LastName = "Time",
            EmailAddress = "invalid@xcel.com",
        };
        await PersonsRepository.AddAsync(person);
        await PersonsRepository.SaveChangesAsync();

        var date = FakeTimeProvider.GetUtcNow().UtcDateTime.Date;
        var start = date.AddHours(11); // 11:00 AM UTC
        var end = date.AddHours(10); // 10:00 AM UTC

        var input = new OneOffAvailabilityInput(
            person.Id,
            AvailabilityOwnerType.Reviewer,
            start,
            end
        );

        // Act
        var result = await _command.ExecuteAsync(input);

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(AddOneOffAvailabilitySlotCommandErrors.InvalidTimeRange, error);

        // Verify no rules were added (validation happens before repository interaction)
        var rules = await AvailabilityRulesRepository.GetRulesActiveOnDateAsync(person.Id, date);
        Assert.Empty(rules);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenPersonDoesNotExist()
    {
        // Scenario: Call AddOneOffAvailabilitySlotCommand for a non-existent person.
        // Arrange
        var date = FakeTimeProvider.GetUtcNow().UtcDateTime.Date;
        var start = date.AddHours(9);
        var end = date.AddHours(10);

        var input = new OneOffAvailabilityInput(
            Guid.NewGuid(), // Non-existent ID
            AvailabilityOwnerType.Tutor,
            start,
            end
        );

        // Act
        var result = await _command.ExecuteAsync(input);

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(AddOneOffAvailabilitySlotCommandErrors.PersonNotFound(input.OwnerId), error);

        // Verify no rules were added
        var rules = await AvailabilityRulesRepository.GetRulesActiveOnDateAsync(
            input.OwnerId,
            date
        );
        Assert.Empty(rules);
    }
}
