using Application.UseCases.Commands.Availability;
using Domain.Entities;
using Domain.Results; // Using Domain.Primitives for Result and Error
using Xcel.TestUtils; // Assuming Xcel.TestUtils namespace

namespace Domain.IntegrationTests.UseCases.Commands.Availability;

// Integration tests for SetAvailabilityRulesCommand
public class SetAvailabilityRulesCommandTests : BaseTest
{
    private ISetAvailabilityRulesCommand _command = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _command = new SetAvailabilityRulesCommand(
            AvailabilityRulesRepository, // AvailabilityRulesRepository available from BaseTest
            PersonsRepository, // PersonsRepository available from BaseTest
            CreateLogger<SetAvailabilityRulesCommand>() // CreateLogger available from BaseTest
        );
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReplaceExistingStandardAvailabilityRules_WhenInputIsValid()
    {
        // Scenario: Call SetAvailabilityRulesCommand with valid input. It should delete existing standard availability rules and add the new ones, leaving other rule types untouched.
        // Arrange
        var person = new Person
        {
            FirstName = "Set",
            LastName = "Rules",
            EmailAddress = "setrules@xcel.com",
        };
        await PersonsRepository.AddAsync(person);
        await PersonsRepository.SaveChangesAsync();

        // Add some existing rules first (including one-off and exclusion that should NOT be deleted)
        var date = FakeTimeProvider.GetUtcNow().UtcDateTime.Date; // FakeTimeProvider available from BaseTest
        var ownerId = person.Id;
        var ownerType = AvailabilityOwnerType.Tutor;

        var existingStandardRule = new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Owner = person,
            OwnerType = ownerType,
            RuleType = AvailabilityRuleType.AvailabilityStandard, // Existing standard rule
            DayOfWeek = DayOfWeek.Monday,
            StartTimeUtc = TimeSpan.FromHours(8),
            EndTimeUtc = TimeSpan.FromHours(9),
            ActiveFromUtc = date,
            ActiveUntilUtc = null,
        };
        var existingOneOffRule = new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Owner = person,
            OwnerType = ownerType,
            RuleType = AvailabilityRuleType.AvailabilityOneOff, // Existing one-off rule (should NOT be deleted)
            DayOfWeek = date.AddDays(1).DayOfWeek,
            StartTimeUtc = TimeSpan.FromHours(10),
            EndTimeUtc = TimeSpan.FromHours(11),
            ActiveFromUtc = date.AddDays(1),
            ActiveUntilUtc = date.AddDays(1),
        };
        var existingExclusionRule = new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Owner = person,
            OwnerType = ownerType,
            RuleType = AvailabilityRuleType.ExclusionFullDay, // Existing exclusion rule (should NOT be deleted)
            DayOfWeek = date.AddDays(2).DayOfWeek,
            StartTimeUtc = TimeSpan.Zero,
            EndTimeUtc = TimeSpan.FromDays(1),
            ActiveFromUtc = date.AddDays(2),
            ActiveUntilUtc = date.AddDays(2),
        };

        await AvailabilityRulesRepository.AddRangeAsync([existingStandardRule, existingOneOffRule, existingExclusionRule]); // AvailabilityRulesRepository available from BaseTest
        await AvailabilityRulesRepository.SaveChangesAsync();

        // Define the new set of STANDARD availability rules (AvailabilityRuleInput no longer has IsExcluded)
        var newRulesInput = new List<AvailabilityRuleInput>
        {
            new AvailabilityRuleInput(
                DayOfWeek.Monday,
                TimeSpan.FromHours(14),
                TimeSpan.FromHours(16),
                date,
                null
            ), // New Recurring Mon (Standard)
            new AvailabilityRuleInput(
                DayOfWeek.Tuesday,
                TimeSpan.FromHours(9),
                TimeSpan.FromHours(10),
                date,
                date
            ), // New One-off Tue (Standard - Note: This command *sets* Standard rules, even if the input looks like a one-off date range. The RuleType will be AvailabilityStandard)
        };

        // Act
        var result = await _command.ExecuteAsync(
            ownerId,
            ownerType,
            newRulesInput
        );

        // Assert
        Assert.True(result.IsSuccess);

        // Verify the existing standard rule is deleted
        var oldStandardRule = await AvailabilityRulesRepository.GetByIdAsync(existingStandardRule.Id);
        Assert.Null(oldStandardRule);

        // Verify the existing one-off and exclusion rules are NOT deleted
        var oldOneOffRule = await AvailabilityRulesRepository.GetByIdAsync(existingOneOffRule.Id);
        var oldExclusionRule = await AvailabilityRulesRepository.GetByIdAsync(existingExclusionRule.Id);
        Assert.NotNull(oldOneOffRule);
        Assert.NotNull(oldExclusionRule);

        // Verify the new rules are added
        var allRules = await AvailabilityRulesRepository.GetByOwnerAsync(
            ownerId,
            ownerType
        );

        // Expected rules: 2 new standard rules + 1 existing one-off + 1 existing exclusion = 4 total
        Assert.Equal(4, allRules.Count);

        // Basic check for properties and RuleType of added rules
        Assert.Contains(
            allRules,
            r =>
                r.DayOfWeek == DayOfWeek.Monday
                && r.StartTimeUtc == TimeSpan.FromHours(14)
                && r.EndTimeUtc == TimeSpan.FromHours(16)
                && !r.ActiveUntilUtc.HasValue
                && r.RuleType == AvailabilityRuleType.AvailabilityStandard // Should be Standard
        );
        Assert.Contains(
            allRules,
            r =>
                r.DayOfWeek == DayOfWeek.Tuesday
                && r.StartTimeUtc == TimeSpan.FromHours(9)
                && r.EndTimeUtc == TimeSpan.FromHours(10)
                && r.ActiveFromUtc.Date == date.Date
                && r.ActiveUntilUtc.Value.Date == date.Date
                && r.RuleType == AvailabilityRuleType.AvailabilityStandard // Should be Standard
        );

        // Verify the existing rules are still present with their original types
        Assert.Contains(
             allRules,
             r => r.Id == existingOneOffRule.Id && r.RuleType == AvailabilityRuleType.AvailabilityOneOff
         );
        Assert.Contains(
             allRules,
             r => r.Id == existingExclusionRule.Id && r.RuleType == AvailabilityRuleType.ExclusionFullDay
         );
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenPersonDoesNotExist()
    {
        // Scenario: Call SetAvailabilityRulesCommand for a non-existent person.
        // Arrange
        var nonExistentPersonId = Guid.NewGuid();
        var newRulesInput = new List<AvailabilityRuleInput>
        {
            new AvailabilityRuleInput(
                DayOfWeek.Monday,
                TimeSpan.FromHours(14),
                TimeSpan.FromHours(16),
                FakeTimeProvider.GetUtcNow().UtcDateTime.Date,
                null
            ),
        };

        // Act
        var result = await _command.ExecuteAsync(
            nonExistentPersonId,
            AvailabilityOwnerType.Tutor,
            newRulesInput
        );

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(SetAvailabilityRulesCommandErrors.PersonNotFound(nonExistentPersonId), error);

        // Verify no rules were added or deleted
        var rules = await AvailabilityRulesRepository.GetByOwnerAsync(
            nonExistentPersonId,
            AvailabilityOwnerType.Tutor
        );
        Assert.Empty(rules);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenRulesListIsEmpty()
    {
        // Scenario: Call SetAvailabilityRulesCommand with an empty list of rules.
        // Arrange
        var person = new Person
        {
            FirstName = "Set",
            LastName = "Empty",
            EmailAddress = "setempty@xcel.com",
        };
        await PersonsRepository.AddAsync(person);
        await PersonsRepository.SaveChangesAsync();

        var emptyRulesInput = new List<AvailabilityRuleInput>();

        // Act
        var result = await _command.ExecuteAsync(
            person.Id,
            AvailabilityOwnerType.Tutor,
            emptyRulesInput
        );

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(ErrorType.Validation, error.Type);
        Assert.Equal("At least one rule must be submitted.", error.Message);

        // Verify no rules were added or deleted
        var rules = await AvailabilityRulesRepository.GetByOwnerAsync(
            person.Id,
            AvailabilityOwnerType.Tutor
        );
        Assert.Empty(rules);
    }

     [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenSubmittedRulesHaveOverlappingTimeRanges()
    {
        // Scenario: Call SetAvailabilityRulesCommand with a list of new standard availability rules that overlap for the same day/period.
        // Arrange
        var person = new Person
        {
            FirstName = "Set",
            LastName = "Overlap",
            EmailAddress = "setoverlap@xcel.com",
        };
        await PersonsRepository.AddAsync(person);
        await PersonsRepository.SaveChangesAsync();

        var date = FakeTimeProvider.GetUtcNow().UtcDateTime.Date;

        // Define overlapping standard availability rules for the same Monday
        var overlappingRulesInput = new List<AvailabilityRuleInput>
        {
            new AvailabilityRuleInput(
                DayOfWeek.Monday,
                TimeSpan.FromHours(9),
                TimeSpan.FromHours(11), // 9:00 - 11:00
                date,
                null
            ),
            new AvailabilityRuleInput(
                DayOfWeek.Monday,
                TimeSpan.FromHours(10),
                TimeSpan.FromHours(12), // 10:00 - 12:00 (overlaps with 9-11)
                date,
                null
            ),
        };

        // Act
        var result = await _command.ExecuteAsync(
            person.Id,
            AvailabilityOwnerType.Tutor,
            overlappingRulesInput
        );

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(SetAvailabilityRulesCommandErrors.OverlappingAvailabilityRules, error);

        // Verify no rules were added (since the command failed before saving)
        var rules = await AvailabilityRulesRepository.GetByOwnerAsync(
            person.Id,
            AvailabilityOwnerType.Tutor
        );
        Assert.Empty(rules);
    }

     [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenSubmittedRulesHaveInvalidTimeRange()
    {
        // Scenario: Call SetAvailabilityRulesCommand with a standard availability rule having an invalid time range (Start >= End).
        // Arrange
        var person = new Person
        {
            FirstName = "Set",
            LastName = "InvalidTime",
            EmailAddress = "setinvalidtime@xcel.com",
        };
        await PersonsRepository.AddAsync(person);
        await PersonsRepository.SaveChangesAsync();

        var date = FakeTimeProvider.GetUtcNow().UtcDateTime.Date;

        // Define a rule with an invalid time range
        var invalidTimeRuleInput = new List<AvailabilityRuleInput>
        {
            new AvailabilityRuleInput(
                DayOfWeek.Wednesday,
                TimeSpan.FromHours(11),
                TimeSpan.FromHours(10), // Invalid: Start > End
                date,
                null
            ),
        };

        // Act
        var result = await _command.ExecuteAsync(
            person.Id,
            AvailabilityOwnerType.Tutor,
            invalidTimeRuleInput
        );

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(ErrorType.Validation, error.Type);
        Assert.Contains("Start time (11:00:00) must be before end time (10:00:00).", error.Message); // Check for specific message part

        // Verify no rules were added
        var rules = await AvailabilityRulesRepository.GetByOwnerAsync(
            person.Id,
            AvailabilityOwnerType.Tutor
        );
        Assert.Empty(rules);
    }
}
