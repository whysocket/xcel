using Application.UseCases.Queries.Availability;
using Domain.Entities;
using Xcel.TestUtils;

namespace Domain.IntegrationTests.UseCases.Queries.Availability;

public class GetAvailabilityRulesQueryTests : BaseTest
{
    private IGetAvailabilityRulesQuery _query = null!;
    private Person _testOwner = null!;
    private Person _otherOwner = null!; // For testing filtering

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _query = new GetAvailabilityRulesQuery(
            AvailabilityRulesRepository,
            CreateLogger<GetAvailabilityRulesQuery>()
        );

        // Create test persons
        _testOwner = new Person
        {
            FirstName = "Rule",
            LastName = "Owner",
            EmailAddress = "rule.owner@xcel.com",
            Id = Guid.NewGuid(),
        };
        _otherOwner = new Person
        {
            FirstName = "Other",
            LastName = "Owner",
            EmailAddress = "other.owner@xcel.com",
            Id = Guid.NewGuid(),
        };
        await PersonsRepository.AddRangeAsync(new[] { _testOwner, _otherOwner });
        await PersonsRepository.SaveChangesAsync();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnAllRules_ForOwnerWithMultipleRuleTypes()
    {
        // Scenario: Query for rules for an owner who has various types of rules configured (recurring, one-off, exclusion).
        // Arrange
        var today = FakeTimeProvider.GetUtcNow().UtcDateTime.Date;
        var ownerId = _testOwner.Id;
        var ownerType = AvailabilityOwnerType.Reviewer;

        // Add various rules for the test owner
        var recurringRule = new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Owner = _testOwner,
            OwnerType = ownerType,
            DayOfWeek = DayOfWeek.Monday,
            StartTimeUtc = TimeSpan.FromHours(9),
            EndTimeUtc = TimeSpan.FromHours(17),
            ActiveFromUtc = today,
            ActiveUntilUtc = null,
            IsExcluded = false,
        };
        var oneOffRule = new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Owner = _testOwner,
            OwnerType = ownerType,
            DayOfWeek = today.AddDays(3).DayOfWeek,
            StartTimeUtc = TimeSpan.FromHours(10),
            EndTimeUtc = TimeSpan.FromHours(12),
            ActiveFromUtc = today.AddDays(3),
            ActiveUntilUtc = today.AddDays(3),
            IsExcluded = false,
        };
        var exclusionRule = new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Owner = _testOwner,
            OwnerType = ownerType,
            DayOfWeek = today.AddDays(5).DayOfWeek,
            StartTimeUtc = TimeSpan.Zero,
            EndTimeUtc = TimeSpan.Zero,
            ActiveFromUtc = today.AddDays(5),
            ActiveUntilUtc = today.AddDays(5),
            IsExcluded = true,
        };
        var expiringRule = new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Owner = _testOwner,
            OwnerType = ownerType,
            DayOfWeek = DayOfWeek.Tuesday,
            StartTimeUtc = TimeSpan.FromHours(14),
            EndTimeUtc = TimeSpan.FromHours(16),
            ActiveFromUtc = today,
            ActiveUntilUtc = today.AddDays(7),
            IsExcluded = false,
        };

        await AvailabilityRulesRepository.AddRangeAsync(
            new[] { recurringRule, oneOffRule, exclusionRule, expiringRule }
        );
        await AvailabilityRulesRepository.SaveChangesAsync();

        // Act
        var result = await _query.ExecuteAsync(ownerId, ownerType);

        // Assert
        Assert.True(result.IsSuccess);
        var ruleDtos = result.Value;

        // Expected: All 4 rules added should be returned as DTOs
        Assert.Equal(4, ruleDtos.Count);

        // Verify key properties of the DTOs match the added rules
        Assert.Contains(
            ruleDtos,
            dto =>
                dto.Id == recurringRule.Id
                && dto.DayOfWeek == DayOfWeek.Monday
                && !dto.ActiveUntilUtc.HasValue
                && !dto.IsExcluded
        );
        Assert.Contains(
            ruleDtos,
            dto =>
                dto.Id == oneOffRule.Id
                && dto.ActiveFromUtc.Date == today.AddDays(3)
                && dto.ActiveUntilUtc!.Value.Date == today.AddDays(3)
                && !dto.IsExcluded
        );
        Assert.Contains(
            ruleDtos,
            dto => dto.Id == exclusionRule.Id && dto.IsExcluded && dto.StartTimeUtc == TimeSpan.Zero
        );
        Assert.Contains(
            ruleDtos,
            dto =>
                dto.Id == expiringRule.Id
                && dto.ActiveUntilUtc.HasValue
                && dto.ActiveUntilUtc.Value.Date == today.AddDays(7)
                && !dto.IsExcluded
        );
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnEmptyList_WhenOwnerHasNoRules()
    {
        // Scenario: Query for rules for an owner who has no rules configured.
        // Arrange
        var ownerId = _testOwner.Id; // Use the test owner, but don't add rules for them in this test
        var ownerType = AvailabilityOwnerType.Tutor;

        // Act
        var result = await _query.ExecuteAsync(ownerId, ownerType);

        // Assert
        Assert.True(result.IsSuccess);
        var ruleDtos = result.Value;

        // Expected: Empty list
        Assert.Empty(ruleDtos);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnOnlyCorrectOwnersRules_WhenRulesExistForOtherOwnersAndTypes()
    {
        // Scenario: Query for rules for a specific owner/type, ensuring rules belonging to other owners or different types for the same owner are NOT returned.
        // Arrange
        var today = FakeTimeProvider.GetUtcNow().UtcDateTime.Date;
        var testOwnerId = _testOwner.Id;
        var otherOwnerId = _otherOwner.Id;

        // Rules for the TARGET owner (Reviewer) - these should be returned
        var targetRule1 = new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            OwnerId = testOwnerId,
            Owner = _testOwner,
            OwnerType = AvailabilityOwnerType.Reviewer,
            DayOfWeek = DayOfWeek.Monday,
            StartTimeUtc = TimeSpan.FromHours(9),
            EndTimeUtc = TimeSpan.FromHours(10),
            ActiveFromUtc = today,
            IsExcluded = false,
        };
        var targetRule2 = new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            OwnerId = testOwnerId,
            Owner = _testOwner,
            OwnerType = AvailabilityOwnerType.Reviewer,
            DayOfWeek = DayOfWeek.Wednesday,
            StartTimeUtc = TimeSpan.FromHours(14),
            EndTimeUtc = TimeSpan.FromHours(15),
            ActiveFromUtc = today,
            IsExcluded = false,
        };

        // Rules for the TARGET owner but DIFFERENT type (Tutor) - these should NOT be returned by the query for Reviewer
        var sameOwnerOtherTypeRule = new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            OwnerId = testOwnerId,
            Owner = _testOwner,
            OwnerType = AvailabilityOwnerType.Tutor,
            DayOfWeek = DayOfWeek.Friday,
            StartTimeUtc = TimeSpan.FromHours(16),
            EndTimeUtc = TimeSpan.FromHours(17),
            ActiveFromUtc = today,
            IsExcluded = false,
        };

        // Rules for a DIFFERENT owner (Reviewer) - these should NOT be returned
        var otherOwnerRule = new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            OwnerId = otherOwnerId,
            Owner = _otherOwner,
            OwnerType = AvailabilityOwnerType.Reviewer,
            DayOfWeek = DayOfWeek.Monday,
            StartTimeUtc = TimeSpan.FromHours(8),
            EndTimeUtc = TimeSpan.FromHours(9),
            ActiveFromUtc = today,
            IsExcluded = false,
        };

        await AvailabilityRulesRepository.AddRangeAsync(
            new[] { targetRule1, targetRule2, sameOwnerOtherTypeRule, otherOwnerRule }
        );
        await AvailabilityRulesRepository.SaveChangesAsync();

        // Act
        // Query for the TARGET owner and TARGET type (Reviewer)
        var result = await _query.ExecuteAsync(testOwnerId, AvailabilityOwnerType.Reviewer);

        // Assert
        Assert.True(result.IsSuccess);
        var ruleDtos = result.Value;

        // Expected: Only the two target rules should be returned
        Assert.Equal(2, ruleDtos.Count);

        // Verify the IDs match the expected rules
        Assert.Contains(ruleDtos, dto => dto.Id == targetRule1.Id);
        Assert.Contains(ruleDtos, dto => dto.Id == targetRule2.Id);

        // Explicitly verify the IDs of the other rules are NOT present
        Assert.DoesNotContain(ruleDtos, dto => dto.Id == sameOwnerOtherTypeRule.Id);
        Assert.DoesNotContain(ruleDtos, dto => dto.Id == otherOwnerRule.Id);
    }
}
