using Application.UseCases.Commands.Availability;
using Domain.Entities;
using Xcel.TestUtils;

namespace Domain.IntegrationTests.UseCases.Commands.Availability;

public class UpdateAvailabilityRuleCommandTests : BaseTest
{
    private IUpdateAvailabilityRuleCommand _command = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _command = new UpdateAvailabilityRuleCommand(
            AvailabilityRulesRepository,
            CreateLogger<UpdateAvailabilityRuleCommand>()
        );
    }

    [Fact]
    public async Task ExecuteAsync_ShouldUpdateRule_WhenValid()
    {
        // Arrange
        var person = new Person
        {
            FirstName = "Updater",
            LastName = "User",
            EmailAddress = "update@xcel.com",
        };
        await PersonsRepository.AddAsync(person);
        await PersonsRepository.SaveChangesAsync();

        var rule = new AvailabilityRule
        {
            OwnerId = person.Id,
            Owner = person,
            OwnerType = AvailabilityOwnerType.Tutor,
            DayOfWeek = DayOfWeek.Monday,
            StartTimeUtc = new(10, 0, 0),
            EndTimeUtc = new(12, 0, 0),
            ActiveFromUtc = FakeTimeProvider.GetUtcNow().UtcDateTime.Date,
            ActiveUntilUtc = null,
            IsExcluded = false,
        };

        await AvailabilityRulesRepository.AddAsync(rule);
        await AvailabilityRulesRepository.SaveChangesAsync();

        var input = new UpdateAvailabilityRuleInput(
            rule.Id,
            person.Id,
            AvailabilityOwnerType.Tutor,
            new(9, 0, 0),
            new(11, 0, 0),
            FakeTimeProvider.GetUtcNow().UtcDateTime.Date,
            null,
            false
        );

        // Act
        var result = await _command.ExecuteAsync(input);

        // Assert
        Assert.True(result.IsSuccess);
        var updated = await AvailabilityRulesRepository.GetByIdAsync(rule.Id);
        Assert.Equal(new TimeSpan(9, 0, 0), updated!.StartTimeUtc);
        Assert.Equal(new TimeSpan(11, 0, 0), updated.EndTimeUtc);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenRuleNotFound()
    {
        // Arrange
        var input = new UpdateAvailabilityRuleInput(
            Guid.NewGuid(),
            Guid.NewGuid(),
            AvailabilityOwnerType.Tutor,
            new(9, 0, 0),
            new(11, 0, 0),
            FakeTimeProvider.GetUtcNow().UtcDateTime.Date,
            null,
            false
        );

        // Act
        var result = await _command.ExecuteAsync(input);

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(UpdateAvailabilityRuleCommandErrors.RuleNotFound(input.RuleId), error);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenUnauthorized()
    {
        // Arrange
        var person = new Person
        {
            FirstName = "Wrong",
            LastName = "Owner",
            EmailAddress = "wrong@xcel.com",
        };
        await PersonsRepository.AddAsync(person);
        await PersonsRepository.SaveChangesAsync();

        var rule = new AvailabilityRule
        {
            OwnerId = person.Id,
            Owner = person,
            OwnerType = AvailabilityOwnerType.Tutor,
            DayOfWeek = DayOfWeek.Tuesday,
            StartTimeUtc = new(10, 0, 0),
            EndTimeUtc = new(12, 0, 0),
            ActiveFromUtc = FakeTimeProvider.GetUtcNow().UtcDateTime.Date,
            ActiveUntilUtc = null,
            IsExcluded = false,
        };

        await AvailabilityRulesRepository.AddAsync(rule);
        await AvailabilityRulesRepository.SaveChangesAsync();

        var input = new UpdateAvailabilityRuleInput(
            rule.Id,
            Guid.NewGuid(), // wrong owner
            AvailabilityOwnerType.Tutor,
            new(9, 0, 0),
            new(11, 0, 0),
            FakeTimeProvider.GetUtcNow().UtcDateTime.Date,
            null,
            false
        );

        // Act
        var result = await _command.ExecuteAsync(input);

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(UpdateAvailabilityRuleCommandErrors.Unauthorized(rule.Id), error);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenTimeRangeInvalid()
    {
        // Arrange
        var person = new Person
        {
            FirstName = "Time",
            LastName = "Fail",
            EmailAddress = "time@xcel.com",
        };
        await PersonsRepository.AddAsync(person);
        await PersonsRepository.SaveChangesAsync();

        var rule = new AvailabilityRule
        {
            OwnerId = person.Id,
            Owner = person,
            OwnerType = AvailabilityOwnerType.Tutor,
            DayOfWeek = DayOfWeek.Friday,
            StartTimeUtc = new(9, 0, 0),
            EndTimeUtc = new(11, 0, 0),
            ActiveFromUtc = FakeTimeProvider.GetUtcNow().UtcDateTime.Date,
            ActiveUntilUtc = null,
            IsExcluded = false,
        };

        await AvailabilityRulesRepository.AddAsync(rule);
        await AvailabilityRulesRepository.SaveChangesAsync();

        var input = new UpdateAvailabilityRuleInput(
            rule.Id,
            person.Id,
            AvailabilityOwnerType.Tutor,
            new(13, 0, 0), // invalid: start > end
            new(10, 0, 0),
            FakeTimeProvider.GetUtcNow().UtcDateTime.Date,
            null,
            false
        );

        // Act
        var result = await _command.ExecuteAsync(input);

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(UpdateAvailabilityRuleCommandErrors.InvalidTimeRange, error);
    }
}
