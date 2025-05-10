using Application.UseCases.Commands.Availability;
using Domain.Entities;
using Xcel.TestUtils; // Assuming Xcel.TestUtils namespace

namespace Domain.IntegrationTests.UseCases.Commands.Availability;

// Integration tests for DeleteAvailabilityRuleCommand
public class DeleteAvailabilityRuleCommandTests : BaseTest
{
    private IDeleteAvailabilityRuleCommand _command = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _command = new DeleteAvailabilityRuleCommand(
            AvailabilityRulesRepository, // AvailabilityRulesRepository available from BaseTest
            CreateLogger<DeleteAvailabilityRuleCommand>() // CreateLogger available from BaseTest
        );
    }

    [Fact]
    public async Task ExecuteAsync_ShouldDeleteRule_WhenRuleExistsAndOwnerMatches()
    {
        // Scenario: Call DeleteAvailabilityRuleCommand for an existing rule where the provided owner ID and type match the rule's owner.
        // Arrange
        var person = new Person
        {
            FirstName = "Delete",
            LastName = "Me",
            EmailAddress = "delete@xcel.com",
        };
        await PersonsRepository.AddAsync(person); // PersonsRepository available from BaseTest
        await PersonsRepository.SaveChangesAsync(); // SaveChangesAsync available from BaseTest

        var date = FakeTimeProvider.GetUtcNow().UtcDateTime.Date; // FakeTimeProvider available from BaseTest
        var ruleToDelete = new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            OwnerId = person.Id,
            Owner = person, // Link the person entity
            OwnerType = AvailabilityOwnerType.Reviewer,
            RuleType = AvailabilityRuleType.AvailabilityStandard, // Use RuleType instead of IsExcluded
            DayOfWeek = date.DayOfWeek,
            StartTimeUtc = TimeSpan.FromHours(10),
            EndTimeUtc = TimeSpan.FromHours(12),
            ActiveFromUtc = date,
            ActiveUntilUtc = date.AddDays(7), // A recurring rule example
            // IsExcluded is removed
        };
        await AvailabilityRulesRepository.AddAsync(ruleToDelete); // AvailabilityRulesRepository available from BaseTest
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
        // Scenario: Call DeleteAvailabilityRuleCommand with a rule ID that does not exist.
        // Arrange
        var input = new DeleteAvailabilityRuleInput(
            Guid.NewGuid(), // Non-existent rule ID
            Guid.NewGuid(), // OwnerId (doesn't matter as rule won't be found)
            AvailabilityOwnerType.Tutor // OwnerType (doesn't matter as rule won't be found)
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
        // Scenario: Call DeleteAvailabilityRuleCommand for an existing rule, but the provided owner ID does not match the rule's owner ID.
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
        await PersonsRepository.AddRangeAsync([correctPerson, wrongPerson]);
        await PersonsRepository.SaveChangesAsync();

        var date = FakeTimeProvider.GetUtcNow().UtcDateTime.Date;
        var ruleToDelete = new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            OwnerId = correctPerson.Id, // Rule belongs to correctPerson
            Owner = correctPerson,
            OwnerType = AvailabilityOwnerType.Reviewer,
            RuleType = AvailabilityRuleType.AvailabilityStandard, // Use RuleType
            DayOfWeek = date.DayOfWeek,
            StartTimeUtc = TimeSpan.FromHours(10),
            EndTimeUtc = TimeSpan.FromHours(12),
            ActiveFromUtc = date,
            ActiveUntilUtc = date.AddDays(7),
            // IsExcluded is removed
        };
        await AvailabilityRulesRepository.AddAsync(ruleToDelete);
        await AvailabilityRulesRepository.SaveChangesAsync();

        var input = new DeleteAvailabilityRuleInput(
            ruleToDelete.Id,
            wrongPerson.Id, // Mismatched OwnerId in input
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
        // Scenario: Call DeleteAvailabilityRuleCommand for an existing rule, but the provided owner type does not match the rule's owner type.
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
            RuleType = AvailabilityRuleType.AvailabilityStandard, // Use RuleType
            DayOfWeek = date.DayOfWeek,
            StartTimeUtc = TimeSpan.FromHours(10),
            EndTimeUtc = TimeSpan.FromHours(12),
            ActiveFromUtc = date,
            ActiveUntilUtc = date.AddDays(7),
            // IsExcluded is removed
        };
        await AvailabilityRulesRepository.AddAsync(ruleToDelete);
        await AvailabilityRulesRepository.SaveChangesAsync();

        var input = new DeleteAvailabilityRuleInput(
            ruleToDelete.Id,
            person.Id,
            AvailabilityOwnerType.Reviewer // Input claims Reviewer (Mismatched Type)
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
