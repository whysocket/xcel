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
    public async Task ExecuteAsync_ShouldAddSlot_WhenInputIsValid()
    {
        // Arrange
        var person = new Person
        {
            FirstName = "Slot",
            LastName = "Valid",
            EmailAddress = "slot@xcel.com",
        };
        await PersonsRepository.AddAsync(person);
        await PersonsRepository.SaveChangesAsync();

        var start = FakeTimeProvider.GetUtcNow().UtcDateTime.Date.AddHours(10);
        var end = start.AddMinutes(30);

        var input = new OneOffAvailabilityInput(
            person.Id,
            AvailabilityOwnerType.Reviewer,
            start,
            end
        );

        // Act
        var result = await _command.ExecuteAsync(input);

        // Assert
        Assert.True(result.IsSuccess);
        var rules = await AvailabilityRulesRepository.GetByOwnerAndDateAsync(person.Id, start.Date);
        Assert.Single(rules);
        Assert.False(rules.First().IsExcluded);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenPersonDoesNotExist()
    {
        // Arrange
        var input = new OneOffAvailabilityInput(
            Guid.NewGuid(),
            AvailabilityOwnerType.Tutor,
            FakeTimeProvider.GetUtcNow().UtcDateTime,
            FakeTimeProvider.GetUtcNow().UtcDateTime.AddMinutes(30)
        );

        // Act
        var result = await _command.ExecuteAsync(input);

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(AddOneOffAvailabilitySlotCommandErrors.PersonNotFound(input.OwnerId), error);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenTimeRangeIsInvalid()
    {
        // Arrange
        var person = new Person
        {
            FirstName = "Bad",
            LastName = "Range",
            EmailAddress = "range@xcel.com",
        };
        await PersonsRepository.AddAsync(person);
        await PersonsRepository.SaveChangesAsync();

        var now = FakeTimeProvider.GetUtcNow().UtcDateTime;
        var input = new OneOffAvailabilityInput(
            person.Id,
            AvailabilityOwnerType.Reviewer,
            now.AddMinutes(30),
            now
        );

        // Act
        var result = await _command.ExecuteAsync(input);

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(AddOneOffAvailabilitySlotCommandErrors.InvalidTimeRange, error);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenSlotOverlaps()
    {
        // Arrange
        var person = new Person
        {
            FirstName = "Overlap",
            LastName = "Slot",
            EmailAddress = "overlap@xcel.com",
        };
        await PersonsRepository.AddAsync(person);
        await PersonsRepository.SaveChangesAsync();

        var date = FakeTimeProvider.GetUtcNow().UtcDateTime.Date;
        var rule = new AvailabilityRule
        {
            OwnerId = person.Id,
            Owner = person,
            OwnerType = AvailabilityOwnerType.Reviewer,
            DayOfWeek = date.DayOfWeek,
            StartTimeUtc = new TimeSpan(10, 0, 0),
            EndTimeUtc = new TimeSpan(11, 0, 0),
            ActiveFromUtc = date,
            ActiveUntilUtc = date,
            IsExcluded = false,
        };

        await AvailabilityRulesRepository.AddAsync(rule);
        await AvailabilityRulesRepository.SaveChangesAsync();

        var input = new OneOffAvailabilityInput(
            person.Id,
            AvailabilityOwnerType.Reviewer,
            date.AddHours(10).AddMinutes(15),
            date.AddHours(10).AddMinutes(45)
        );

        // Act
        var result = await _command.ExecuteAsync(input);

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(AddOneOffAvailabilitySlotCommandErrors.OverlappingSlot, error);
    }
}
