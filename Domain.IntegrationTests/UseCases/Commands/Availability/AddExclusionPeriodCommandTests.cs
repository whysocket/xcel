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
        _command = new AddExclusionPeriodCommand(AvailabilityRulesRepository, PersonsRepository, CreateLogger<AddExclusionPeriodCommand>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldAddExclusions_WhenDatesAreValid()
    {
        // Arrange
        var person = new Person { FirstName = "Blocked", LastName = "Days", EmailAddress = "blocked@xcel.com" };
        await PersonsRepository.AddAsync(person);
        await PersonsRepository.SaveChangesAsync();

        var from = FakeTimeProvider.GetUtcNow().UtcDateTime.Date;
        var to = from.AddDays(2);

        var input = new ExclusionPeriodInput(person.Id, AvailabilityOwnerType.Reviewer, from, to);

        // Act
        var result = await _command.ExecuteAsync(input);

        // Assert
        Assert.True(result.IsSuccess);

        var rules = await AvailabilityRulesRepository.GetByOwnerAndDateRangeAsync(person.Id, AvailabilityOwnerType.Reviewer, from, to);
        Assert.Equal(3, rules.Count);
        Assert.All(rules, r => Assert.True(r.IsExcluded));
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenPersonDoesNotExist()
    {
        // Arrange
        var input = new ExclusionPeriodInput(Guid.NewGuid(), AvailabilityOwnerType.Tutor, FakeTimeProvider.GetUtcNow().UtcDateTime, FakeTimeProvider.GetUtcNow().UtcDateTime);

        // Act
        var result = await _command.ExecuteAsync(input);

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(AddExclusionPeriodCommandErrors.PersonNotFound(input.OwnerId), error);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenStartDateAfterEndDate()
    {
        // Arrange
        var person = new Person { FirstName = "Invalid", LastName = "Range", EmailAddress = "invalid@xcel.com" };
        await PersonsRepository.AddAsync(person);
        await PersonsRepository.SaveChangesAsync();

        var input = new ExclusionPeriodInput(
            person.Id,
            AvailabilityOwnerType.Reviewer,
            FakeTimeProvider.GetUtcNow().UtcDateTime.AddDays(2),
            FakeTimeProvider.GetUtcNow().UtcDateTime);

        // Act
        var result = await _command.ExecuteAsync(input);

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(AddExclusionPeriodCommandErrors.InvalidDateRange, error);
    }
}