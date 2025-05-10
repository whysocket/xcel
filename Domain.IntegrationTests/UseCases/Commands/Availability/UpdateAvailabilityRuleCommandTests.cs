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
    public async Task ExecuteAsync_ShouldUpdateRule_WhenRuleExistsAndOwnerMatches()
    {
        // Arrange
        var person = new Person
        {
            FirstName = "Update",
            LastName = "Rule",
            EmailAddress = "update@xcel.com",
        };
        await PersonsRepository.AddAsync(person);
        await PersonsRepository.SaveChangesAsync();

        var originalDate = FakeTimeProvider.GetUtcNow().UtcDateTime.Date;
        var ruleToUpdate = new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            OwnerId = person.Id,
            Owner = person, // Link the person entity
            OwnerType = AvailabilityOwnerType.Reviewer,
            DayOfWeek = originalDate.DayOfWeek,
            StartTimeUtc = TimeSpan.FromHours(10),
            EndTimeUtc = TimeSpan.FromHours(12),
            ActiveFromUtc = originalDate,
            ActiveUntilUtc = originalDate.AddDays(7),
            IsExcluded = false,
        };
        await AvailabilityRulesRepository.AddAsync(ruleToUpdate);
        await AvailabilityRulesRepository.SaveChangesAsync();

        var newDate = originalDate.AddDays(10); // Change the ActiveFromUtc date
        var input = new UpdateAvailabilityRuleInput(
            ruleToUpdate.Id,
            person.Id,
            AvailabilityOwnerType.Reviewer,
            TimeSpan.FromHours(11), // New Start Time
            TimeSpan.FromHours(13), // New End Time
            newDate, // New ActiveFromUtc
            newDate.AddDays(30), // New ActiveUntilUtc
            true // Change to IsExcluded
        );

        // Act
        var result = await _command.ExecuteAsync(input);

        // Assert
        Assert.True(result.IsSuccess);

        // Verify the rule was updated in the repository
        var updatedRule = await AvailabilityRulesRepository.GetByIdAsync(ruleToUpdate.Id);
        Assert.NotNull(updatedRule);

        Assert.Equal(input.StartTimeUtc, updatedRule.StartTimeUtc);
        Assert.Equal(input.EndTimeUtc, updatedRule.EndTimeUtc);
        Assert.Equal(input.ActiveFromUtc, updatedRule.ActiveFromUtc);
        Assert.Equal(input.ActiveUntilUtc, updatedRule.ActiveUntilUtc);
        Assert.Equal(input.IsExcluded, updatedRule.IsExcluded);
        Assert.Equal(input.ActiveFromUtc.DayOfWeek, updatedRule.DayOfWeek); // Verify DayOfWeek is updated based on ActiveFromUtc
        Assert.Equal(input.OwnerId, updatedRule.OwnerId);
        Assert.Equal(input.OwnerType, updatedRule.OwnerType);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenRuleDoesNotExist()
    {
        // Arrange
        var input = new UpdateAvailabilityRuleInput(
            Guid.NewGuid(), // Non-existent rule ID
            Guid.NewGuid(),
            AvailabilityOwnerType.Tutor,
            TimeSpan.FromHours(9),
            TimeSpan.FromHours(10),
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

        var originalDate = FakeTimeProvider.GetUtcNow().UtcDateTime.Date;
        var ruleToUpdate = new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            OwnerId = correctPerson.Id,
            Owner = correctPerson,
            OwnerType = AvailabilityOwnerType.Reviewer,
            DayOfWeek = originalDate.DayOfWeek,
            StartTimeUtc = TimeSpan.FromHours(10),
            EndTimeUtc = TimeSpan.FromHours(12),
            ActiveFromUtc = originalDate,
            ActiveUntilUtc = originalDate.AddDays(7),
            IsExcluded = false,
        };
        await AvailabilityRulesRepository.AddAsync(ruleToUpdate);
        await AvailabilityRulesRepository.SaveChangesAsync();

        var input = new UpdateAvailabilityRuleInput(
            ruleToUpdate.Id,
            wrongPerson.Id, // Mismatched OwnerId
            AvailabilityOwnerType.Reviewer,
            TimeSpan.FromHours(11),
            TimeSpan.FromHours(13),
            originalDate,
            null,
            false
        );

        // Act
        var result = await _command.ExecuteAsync(input);

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(UpdateAvailabilityRuleCommandErrors.Unauthorized(input.RuleId), error);

        // Verify the rule was NOT updated
        var existingRule = await AvailabilityRulesRepository.GetByIdAsync(ruleToUpdate.Id);
        Assert.NotNull(existingRule);
        Assert.Equal(TimeSpan.FromHours(10), existingRule.StartTimeUtc); // Check original value
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

        var originalDate = FakeTimeProvider.GetUtcNow().UtcDateTime.Date;
        var ruleToUpdate = new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            OwnerId = person.Id,
            Owner = person,
            OwnerType = AvailabilityOwnerType.Tutor, // Rule is for Tutor
            DayOfWeek = originalDate.DayOfWeek,
            StartTimeUtc = TimeSpan.FromHours(10),
            EndTimeUtc = TimeSpan.FromHours(12),
            ActiveFromUtc = originalDate,
            ActiveUntilUtc = originalDate.AddDays(7),
            IsExcluded = false,
        };
        await AvailabilityRulesRepository.AddAsync(ruleToUpdate);
        await AvailabilityRulesRepository.SaveChangesAsync();

        var input = new UpdateAvailabilityRuleInput(
            ruleToUpdate.Id,
            person.Id,
            AvailabilityOwnerType.Reviewer, // Input claims Reviewer
            TimeSpan.FromHours(11),
            TimeSpan.FromHours(13),
            originalDate,
            null,
            false
        );

        // Act
        var result = await _command.ExecuteAsync(input);

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(UpdateAvailabilityRuleCommandErrors.Unauthorized(input.RuleId), error);

        // Verify the rule was NOT updated
        var existingRule = await AvailabilityRulesRepository.GetByIdAsync(ruleToUpdate.Id);
        Assert.NotNull(existingRule);
        Assert.Equal(TimeSpan.FromHours(10), existingRule.StartTimeUtc); // Check original value
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenUpdatingAvailabilityWithInvalidTimeRange()
    {
        // Arrange
        var person = new Person
        {
            FirstName = "Update",
            LastName = "Invalid",
            EmailAddress = "updateinvalid@xcel.com",
        };
        await PersonsRepository.AddAsync(person);
        await PersonsRepository.SaveChangesAsync();

        var originalDate = FakeTimeProvider.GetUtcNow().UtcDateTime.Date;
        var ruleToUpdate = new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            OwnerId = person.Id,
            Owner = person,
            OwnerType = AvailabilityOwnerType.Reviewer,
            DayOfWeek = originalDate.DayOfWeek,
            StartTimeUtc = TimeSpan.FromHours(10),
            EndTimeUtc = TimeSpan.FromHours(12),
            ActiveFromUtc = originalDate,
            ActiveUntilUtc = originalDate.AddDays(7),
            IsExcluded = false, // This is an availability rule
        };
        await AvailabilityRulesRepository.AddAsync(ruleToUpdate);
        await AvailabilityRulesRepository.SaveChangesAsync();

        var input = new UpdateAvailabilityRuleInput(
            ruleToUpdate.Id,
            person.Id,
            AvailabilityOwnerType.Reviewer,
            TimeSpan.FromHours(13), // Invalid Start Time (after end)
            TimeSpan.FromHours(11), // Invalid End Time
            originalDate,
            null,
            false // Still an availability rule
        );

        // Act
        var result = await _command.ExecuteAsync(input);

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(UpdateAvailabilityRuleCommandErrors.InvalidTimeRange, error);

        // Verify the rule was NOT updated
        var existingRule = await AvailabilityRulesRepository.GetByIdAsync(ruleToUpdate.Id);
        Assert.NotNull(existingRule);
        Assert.Equal(TimeSpan.FromHours(10), existingRule.StartTimeUtc); // Check original value
    }

    [Fact]
    public async Task ExecuteAsync_ShouldSucceed_WhenUpdatingExclusionWithInvalidTimeRange()
    {
        // Arrange
        var person = new Person
        {
            FirstName = "Update",
            LastName = "Exclusion Time",
            EmailAddress = "updateexclusiontime@xcel.com",
        };
        await PersonsRepository.AddAsync(person);
        await PersonsRepository.SaveChangesAsync();

        var originalDate = FakeTimeProvider.GetUtcNow().UtcDateTime.Date;
        var ruleToUpdate = new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            OwnerId = person.Id,
            Owner = person,
            OwnerType = AvailabilityOwnerType.Reviewer,
            DayOfWeek = originalDate.DayOfWeek,
            StartTimeUtc = TimeSpan.Zero, // Original times
            EndTimeUtc = TimeSpan.Zero,
            ActiveFromUtc = originalDate,
            ActiveUntilUtc = originalDate,
            IsExcluded = true, // This is an exclusion rule
        };
        await AvailabilityRulesRepository.AddAsync(ruleToUpdate);
        await AvailabilityRulesRepository.SaveChangesAsync();

        var input = new UpdateAvailabilityRuleInput(
            ruleToUpdate.Id,
            person.Id,
            AvailabilityOwnerType.Reviewer,
            TimeSpan.FromHours(13), // Invalid Start Time (after end)
            TimeSpan.FromHours(11), // Invalid End Time
            originalDate,
            originalDate,
            true // Still an exclusion rule
        );

        // Act
        var result = await _command.ExecuteAsync(input);

        // Assert
        Assert.True(result.IsSuccess); // Should succeed because IsExcluded = true bypasses time validation

        // Verify the rule was updated with the new (invalid) times
        var updatedRule = await AvailabilityRulesRepository.GetByIdAsync(ruleToUpdate.Id);
        Assert.NotNull(updatedRule);
        Assert.Equal(TimeSpan.FromHours(13), updatedRule.StartTimeUtc); // Should be updated to the input value
        Assert.Equal(TimeSpan.FromHours(11), updatedRule.EndTimeUtc); // Should be updated to the input value
        Assert.True(updatedRule.IsExcluded);
    }
}
