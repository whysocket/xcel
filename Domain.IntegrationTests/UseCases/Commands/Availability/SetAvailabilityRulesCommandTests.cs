using Application.UseCases.Commands.Availability;
using Domain.Entities;
using Domain.Results;
using Xcel.TestUtils;

namespace Domain.IntegrationTests.UseCases.Commands.Availability;

public class SetAvailabilityRulesCommandTests : BaseTest
{
    private ISetAvailabilityRulesCommand _command = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _command = new SetAvailabilityRulesCommand(
            AvailabilityRulesRepository,
            PersonsRepository,
            CreateLogger<SetAvailabilityRulesCommand>()
        );
    }

    [Fact]
    public async Task ExecuteAsync_ShouldSetRules_WhenInputIsValid()
    {
        // Arrange
        var person = new Person
        {
            FirstName = "Valid",
            LastName = "AfterInterview",
            EmailAddress = "valid@xcel.com",
        };
        await PersonsRepository.AddAsync(person);
        await PersonsRepository.SaveChangesAsync();

        var rules = new List<AvailabilityRuleInput>
        {
            new(
                DayOfWeek.Monday,
                new(9, 0, 0),
                new(12, 0, 0),
                FakeTimeProvider.GetUtcNow().UtcDateTime.Date
            ),
            new(
                DayOfWeek.Wednesday,
                new(14, 0, 0),
                new(16, 0, 0),
                FakeTimeProvider.GetUtcNow().UtcDateTime.Date
            ),
        };

        // Act
        var result = await _command.ExecuteAsync(person.Id, AvailabilityOwnerType.Reviewer, rules);

        // Assert
        Assert.True(result.IsSuccess);
        var stored = await AvailabilityRulesRepository.GetByOwnerAsync(
            person.Id,
            AvailabilityOwnerType.Reviewer
        );
        Assert.Equal(2, stored.Count);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenPersonDoesNotExist()
    {
        // Arrange
        var fakeId = Guid.NewGuid();
        var rules = new List<AvailabilityRuleInput>
        {
            new(
                DayOfWeek.Monday,
                new(9, 0, 0),
                new(12, 0, 0),
                FakeTimeProvider.GetUtcNow().UtcDateTime.Date
            ),
        };

        // Act
        var result = await _command.ExecuteAsync(fakeId, AvailabilityOwnerType.Reviewer, rules);

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(SetAvailabilityRulesCommandErrors.PersonNotFound(fakeId), error);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenNoRulesProvided()
    {
        // Arrange
        var person = new Person
        {
            FirstName = "Empty",
            LastName = "Rules",
            EmailAddress = "empty@xcel.com",
        };
        await PersonsRepository.AddAsync(person);
        await PersonsRepository.SaveChangesAsync();

        var emptyRules = new List<AvailabilityRuleInput>();

        // Act
        var result = await _command.ExecuteAsync(
            person.Id,
            AvailabilityOwnerType.Reviewer,
            emptyRules
        );

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(ErrorType.Validation, error.Type);
    }
}
