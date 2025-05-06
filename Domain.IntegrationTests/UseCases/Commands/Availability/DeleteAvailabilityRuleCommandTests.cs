using Application.UseCases.Commands.Availability;
using Domain.Entities;
using Xcel.TestUtils;

namespace Domain.IntegrationTests.UseCases.Commands.Availability;

public class DeleteAvailabilityRuleCommandTests : BaseTest
{
    private IDeleteAvailabilityRuleCommand _command = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _command = new DeleteAvailabilityRuleCommand(
            AvailabilityRulesRepository,
            CreateLogger<DeleteAvailabilityRuleCommand>()
        );
    }

    [Fact]
    public async Task ExecuteAsync_ShouldDeleteRule_WhenRuleExistsAndIsAuthorized()
    {
        // Arrange
        var person = new Person
        {
            FirstName = "Deletable",
            LastName = "Slot",
            EmailAddress = "delete@xcel.com",
        };
        await PersonsRepository.AddAsync(person);
        await PersonsRepository.SaveChangesAsync();

        var rule = new AvailabilityRule
        {
            OwnerId = person.Id,
            Owner = person,
            OwnerType = AvailabilityOwnerType.Tutor,
            DayOfWeek = DayOfWeek.Monday,
            StartTimeUtc = new TimeSpan(10, 0, 0),
            EndTimeUtc = new TimeSpan(12, 0, 0),
            ActiveFromUtc = FakeTimeProvider.GetUtcNow().UtcDateTime.Date,
            IsExcluded = false,
        };

        await AvailabilityRulesRepository.AddAsync(rule);
        await AvailabilityRulesRepository.SaveChangesAsync();

        var input = new DeleteAvailabilityRuleInput(
            rule.Id,
            person.Id,
            AvailabilityOwnerType.Tutor
        );

        // Act
        var result = await _command.ExecuteAsync(input);

        // Assert
        Assert.True(result.IsSuccess);
        var deleted = await AvailabilityRulesRepository.GetByIdAsync(rule.Id);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenRuleNotFound()
    {
        // Arrange
        var input = new DeleteAvailabilityRuleInput(
            Guid.NewGuid(),
            Guid.NewGuid(),
            AvailabilityOwnerType.Reviewer
        );

        // Act
        var result = await _command.ExecuteAsync(input);

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(DeleteAvailabilityRuleCommandErrors.RuleNotFound(input.RuleId), error);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenUnauthorized()
    {
        // Arrange
        var person = new Person
        {
            FirstName = "Wrong",
            LastName = "User",
            EmailAddress = "wrong@xcel.com",
        };
        var other = new Person
        {
            FirstName = "Other",
            LastName = "Owner",
            EmailAddress = "owner@xcel.com",
        };
        await PersonsRepository.AddRangeAsync([person, other]);
        await PersonsRepository.SaveChangesAsync();

        var rule = new AvailabilityRule
        {
            OwnerId = other.Id,
            Owner = other,
            OwnerType = AvailabilityOwnerType.Tutor,
            DayOfWeek = DayOfWeek.Tuesday,
            StartTimeUtc = new TimeSpan(14, 0, 0),
            EndTimeUtc = new TimeSpan(15, 0, 0),
            ActiveFromUtc = FakeTimeProvider.GetUtcNow().UtcDateTime.Date,
            IsExcluded = false,
        };

        await AvailabilityRulesRepository.AddAsync(rule);
        await AvailabilityRulesRepository.SaveChangesAsync();

        var input = new DeleteAvailabilityRuleInput(
            rule.Id,
            person.Id,
            AvailabilityOwnerType.Tutor
        );

        // Act
        var result = await _command.ExecuteAsync(input);

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(DeleteAvailabilityRuleCommandErrors.Unauthorized(input.RuleId), error);
    }
}
