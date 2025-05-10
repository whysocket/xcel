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
    public async Task ExecuteAsync_ShouldReplaceExistingRules_WhenInputIsValid()
    {
        // Arrange
        var person = new Person
        {
            FirstName = "Set",
            LastName = "Rules",
            EmailAddress = "setrules@xcel.com",
        };
        await PersonsRepository.AddAsync(person);
        await PersonsRepository.SaveChangesAsync();

        // Add some existing rules first
        var date = FakeTimeProvider.GetUtcNow().UtcDateTime.Date;
        var existingRule1 = new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            OwnerId = person.Id,
            Owner = person,
            OwnerType = AvailabilityOwnerType.Tutor,
            DayOfWeek = date.DayOfWeek,
            StartTimeUtc = TimeSpan.FromHours(8),
            EndTimeUtc = TimeSpan.FromHours(9),
            ActiveFromUtc = date,
            ActiveUntilUtc = date,
            IsExcluded = false,
        };
        var existingRule2 = new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            OwnerId = person.Id,
            Owner = person,
            OwnerType = AvailabilityOwnerType.Tutor,
            DayOfWeek = date.AddDays(1).DayOfWeek,
            StartTimeUtc = TimeSpan.FromHours(10),
            EndTimeUtc = TimeSpan.FromHours(11),
            ActiveFromUtc = date,
            ActiveUntilUtc = date,
            IsExcluded = false,
        };
        await AvailabilityRulesRepository.AddRangeAsync(new[] { existingRule1, existingRule2 });
        await AvailabilityRulesRepository.SaveChangesAsync();

        // Define the new set of rules
        var newRulesInput = new List<AvailabilityRuleInput>
        {
            new AvailabilityRuleInput(
                DayOfWeek.Monday,
                TimeSpan.FromHours(14),
                TimeSpan.FromHours(16),
                date,
                null,
                false
            ), // Recurring Mon
            new AvailabilityRuleInput(
                DayOfWeek.Tuesday,
                TimeSpan.FromHours(9),
                TimeSpan.FromHours(10),
                date,
                date,
                false
            ), // One-off Tue
            new AvailabilityRuleInput(
                DayOfWeek.Friday,
                TimeSpan.Zero,
                TimeSpan.Zero,
                date.AddDays(7),
                date.AddDays(7),
                true
            ), // One-off Exclusion next Fri
        };

        // Act
        var result = await _command.ExecuteAsync(
            person.Id,
            AvailabilityOwnerType.Tutor,
            newRulesInput
        );

        // Assert
        Assert.True(result.IsSuccess);

        // Verify old rules are deleted (optional but good check in integration tests)
        var oldRule1 = await AvailabilityRulesRepository.GetByIdAsync(existingRule1.Id);
        var oldRule2 = await AvailabilityRulesRepository.GetByIdAsync(existingRule2.Id);
        Assert.Null(oldRule1);
        Assert.Null(oldRule2);

        // Verify new rules are added
        var allRules = await AvailabilityRulesRepository.GetByOwnerAsync(
            person.Id,
            AvailabilityOwnerType.Tutor
        );

        Assert.Equal(newRulesInput.Count, allRules.Count);

        // Basic check for properties of added rules
        Assert.Contains(
            allRules,
            r =>
                r.DayOfWeek == DayOfWeek.Monday
                && r.StartTimeUtc == TimeSpan.FromHours(14)
                && r.EndTimeUtc == TimeSpan.FromHours(16)
                && !r.ActiveUntilUtc.HasValue
                && !r.IsExcluded
        );
        Assert.Contains(
            allRules,
            r =>
                r.DayOfWeek == DayOfWeek.Tuesday
                && r.StartTimeUtc == TimeSpan.FromHours(9)
                && r.EndTimeUtc == TimeSpan.FromHours(10)
                && r.ActiveFromUtc == date
                && r.ActiveUntilUtc == date
                && !r.IsExcluded
        );
        Assert.Contains(
            allRules,
            r =>
                r.DayOfWeek == DayOfWeek.Friday
                && r.StartTimeUtc == TimeSpan.Zero
                && r.EndTimeUtc == TimeSpan.Zero
                && r.ActiveFromUtc == date.AddDays(7)
                && r.ActiveUntilUtc == date.AddDays(7)
                && r.IsExcluded
        );
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenPersonDoesNotExist()
    {
        // Arrange
        var nonExistentPersonId = Guid.NewGuid();
        var newRulesInput = new List<AvailabilityRuleInput>
        {
            new AvailabilityRuleInput(
                DayOfWeek.Monday,
                TimeSpan.FromHours(14),
                TimeSpan.FromHours(16),
                FakeTimeProvider.GetUtcNow().UtcDateTime.Date,
                null,
                false
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
}
