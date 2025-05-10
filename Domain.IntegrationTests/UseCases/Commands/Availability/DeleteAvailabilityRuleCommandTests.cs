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
    public async Task ExecuteAsync_ShouldDeleteRule_WhenRuleExistsAndOwnerMatches()
    {
        // Arrange
        var person = new Person
        {
            FirstName = "Delete",
            LastName = "Me",
            EmailAddress = "delete@xcel.com",
        };
        await PersonsRepository.AddAsync(person);
        await PersonsRepository.SaveChangesAsync();

        var date = FakeTimeProvider.GetUtcNow().UtcDateTime.Date;
        var ruleToDelete = new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            OwnerId = person.Id,
            Owner = person, // Link the person entity
            OwnerType = AvailabilityOwnerType.Reviewer,
            DayOfWeek = date.DayOfWeek,
            StartTimeUtc = TimeSpan.FromHours(10),
            EndTimeUtc = TimeSpan.FromHours(12),
            ActiveFromUtc = date,
            ActiveUntilUtc = date.AddDays(7), // A recurring rule example
            IsExcluded = false,
        };
        await AvailabilityRulesRepository.AddAsync(ruleToDelete);
        await AvailabilityRulesRepository.SaveChangesAsync();

        var input = new DeleteAvailabilityRuleInput(
            ruleToDelete.Id,
            person.Id,
            AvailabilityOwnerType.Reviewer
        );

        // Act
        var result = await _command.ExecuteAsync(input);

        // Assert
        Assert.True(result.IsSuccess);

        // Verify the rule is no longer in the repository
        var deletedRule = await AvailabilityRulesRepository.GetByIdAsync(ruleToDelete.Id);
        Assert.Null(deletedRule);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenRuleDoesNotExist()
    {
        // Arrange
        var input = new DeleteAvailabilityRuleInput(
            Guid.NewGuid(), // Non-existent rule ID
            Guid.NewGuid(), // Doesn't matter as rule won't be found
            AvailabilityOwnerType.Tutor
        );

        // Act
        var result = await _command.ExecuteAsync(input);

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(DeleteAvailabilityRuleCommandErrors.RuleNotFound(input.RuleId), error);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenRuleExistsButOwnerIdMismatches()
    {
        // Arrange
        var correctPerson = new Person
        {
            FirstName = "Correct",
            LastName = "Owner",
            EmailAddress = "correct@xcel.com",
            Id = Guid.NewGuid(),
        };
        var wrongPerson = new Person
        {
            FirstName = "Wrong",
            LastName = "Owner",
            EmailAddress = "wrong@xcel.com",
            Id = Guid.NewGuid(),
        };
        await PersonsRepository.AddRangeAsync(new[] { correctPerson, wrongPerson });
        await PersonsRepository.SaveChangesAsync();

        var date = FakeTimeProvider.GetUtcNow().UtcDateTime.Date;
        var ruleToDelete = new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            OwnerId = correctPerson.Id,
            Owner = correctPerson,
            OwnerType = AvailabilityOwnerType.Reviewer,
            DayOfWeek = date.DayOfWeek,
            StartTimeUtc = TimeSpan.FromHours(10),
            EndTimeUtc = TimeSpan.FromHours(12),
            ActiveFromUtc = date,
            ActiveUntilUtc = date.AddDays(7),
            IsExcluded = false,
        };
        await AvailabilityRulesRepository.AddAsync(ruleToDelete);
        await AvailabilityRulesRepository.SaveChangesAsync();

        var input = new DeleteAvailabilityRuleInput(
            ruleToDelete.Id,
            wrongPerson.Id, // Mismatched OwnerId
            AvailabilityOwnerType.Reviewer
        );

        // Act
        var result = await _command.ExecuteAsync(input);

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(DeleteAvailabilityRuleCommandErrors.Unauthorized(input.RuleId), error);

        // Verify the rule was NOT deleted
        var existingRule = await AvailabilityRulesRepository.GetByIdAsync(ruleToDelete.Id);
        Assert.NotNull(existingRule);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenRuleExistsButOwnerTypeMismatches()
    {
        // Arrange
        var person = new Person
        {
            FirstName = "Mismatched",
            LastName = "Type",
            EmailAddress = "mismatch@xcel.com",
            Id = Guid.NewGuid(),
        };
        await PersonsRepository.AddAsync(person);
        await PersonsRepository.SaveChangesAsync();

        var date = FakeTimeProvider.GetUtcNow().UtcDateTime.Date;
        var ruleToDelete = new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            OwnerId = person.Id,
            Owner = person,
            OwnerType = AvailabilityOwnerType.Tutor, // Rule is for Tutor
            DayOfWeek = date.DayOfWeek,
            StartTimeUtc = TimeSpan.FromHours(10),
            EndTimeUtc = TimeSpan.FromHours(12),
            ActiveFromUtc = date,
            ActiveUntilUtc = date.AddDays(7),
            IsExcluded = false,
        };
        await AvailabilityRulesRepository.AddAsync(ruleToDelete);
        await AvailabilityRulesRepository.SaveChangesAsync();

        var input = new DeleteAvailabilityRuleInput(
            ruleToDelete.Id,
            person.Id,
            AvailabilityOwnerType.Reviewer // Input claims Reviewer
        );

        // Act
        var result = await _command.ExecuteAsync(input);

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(DeleteAvailabilityRuleCommandErrors.Unauthorized(input.RuleId), error);

        // Verify the rule was NOT deleted
        var existingRule = await AvailabilityRulesRepository.GetByIdAsync(ruleToDelete.Id);
        Assert.NotNull(existingRule);
    }
}
