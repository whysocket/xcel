using Application.UseCases.Commands.Availability;
using Domain.Entities;
using Xcel.TestUtils;

namespace Domain.IntegrationTests.UseCases.Commands.Availability;

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

        var rules = await AvailabilityRulesRepository.GetByOwnerAndDateAsync(person.Id, date);
        Assert.Single(rules);
        var addedRule = rules.Single();

        Assert.False(addedRule.IsExcluded);
        Assert.Equal(TimeSpan.FromHours(9), addedRule.StartTimeUtc);
        Assert.Equal(TimeSpan.FromHours(10), addedRule.EndTimeUtc);
        Assert.Equal(date, addedRule.ActiveFromUtc);
        Assert.Equal(date, addedRule.ActiveUntilUtc);
        Assert.Equal(person.Id, addedRule.OwnerId);
        Assert.Equal(AvailabilityOwnerType.Tutor, addedRule.OwnerType);
        Assert.Equal(date.DayOfWeek, addedRule.DayOfWeek);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenSlotOverlapsWithExistingAvailability()
    {
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

        // Add an existing rule from 9:00 to 10:00
        var existingRule = new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            OwnerId = person.Id,
            Owner = person, // Link the person entity if required by EF setup
            OwnerType = AvailabilityOwnerType.Tutor,
            DayOfWeek = date.DayOfWeek,
            StartTimeUtc = TimeSpan.FromHours(9),
            EndTimeUtc = TimeSpan.FromHours(10),
            ActiveFromUtc = date,
            ActiveUntilUtc = date,
            IsExcluded = false,
        };
        await AvailabilityRulesRepository.AddAsync(existingRule);
        await AvailabilityRulesRepository.SaveChangesAsync();

        // Input slot from 9:30 to 10:30 (overlaps)
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
        var rules = await AvailabilityRulesRepository.GetByOwnerAndDateAsync(person.Id, date);
        Assert.Single(rules); // Should still only be the original rule
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenStartTimeAtOrAfterEndTime()
    {
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

        // Verify no calls to repository were made after validation fail
        var rules = await AvailabilityRulesRepository.GetByOwnerAndDateAsync(person.Id, date);
        Assert.Empty(rules);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenPersonDoesNotExist()
    {
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
        var rules = await AvailabilityRulesRepository.GetByOwnerAndDateAsync(input.OwnerId, date);
        Assert.Empty(rules);
    }
}
