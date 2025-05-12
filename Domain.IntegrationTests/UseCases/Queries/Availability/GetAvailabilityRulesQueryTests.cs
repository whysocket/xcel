using Application.UseCases.Queries.Availability;
using Domain.Entities;
using Xcel.TestUtils;

namespace Domain.IntegrationTests.UseCases.Queries.Availability;

// Integration tests for GetAvailabilityRulesQuery
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
        await PersonsRepository.AddRangeAsync([_testOwner, _otherOwner]);
        await PersonsRepository.SaveChangesAsync();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnAllRules_ForOwnerWithMultipleRuleTypes_RealWorld()
    {
        // Scenario: Query for rules for an owner who has various types of rules configured (standard, one-off, exclusion) in a real-world context.
        // Arrange
        // Using specific dates for a realistic scenario
        var startDate = new DateTime(2025, 6, 2, 0, 0, 0, DateTimeKind.Utc); // Monday, June 2nd, 2025
        var ownerId = _testOwner.Id;
        var ownerType = AvailabilityOwnerType.Reviewer;

        // Add various rules for the test owner using the new RuleType enum
        // Recurring Monday availability
        var recurringRule = new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Owner = _testOwner,
            OwnerType = ownerType,
            RuleType = AvailabilityRuleType.AvailabilityStandard,
            DayOfWeek = DayOfWeek.Monday, // Applies to Mondays
            StartTimeUtc = TimeSpan.FromHours(9), // 9:00 AM
            EndTimeUtc = TimeSpan.FromHours(13), // 1:00 PM
            ActiveFromUtc = startDate, // Starts from this Monday
            ActiveUntilUtc = null, // No end date
        };

        // One-off availability for a specific Thursday
        var oneOffDate = new DateTime(2025, 6, 5, 0, 0, 0, DateTimeKind.Utc); // Thursday, June 5th, 2025
        var oneOffRule = new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Owner = _testOwner,
            OwnerType = ownerType,
            RuleType = AvailabilityRuleType.AvailabilityOneOff,
            DayOfWeek = oneOffDate.DayOfWeek, // Thursday
            StartTimeUtc = TimeSpan.FromHours(14), // 2:00 PM
            EndTimeUtc = TimeSpan.FromHours(16), // 4:00 PM
            ActiveFromUtc = oneOffDate, // Active only on this date
            ActiveUntilUtc = oneOffDate, // Active only on this date
        };

        // Full-day exclusion for a specific Saturday (e.g., vacation day)
        var fullDayExclusionDate = new DateTime(2025, 6, 7, 0, 0, 0, DateTimeKind.Utc); // Saturday, June 7th, 2025
        var exclusionRule = new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Owner = _testOwner,
            OwnerType = ownerType,
            RuleType = AvailabilityRuleType.ExclusionFullDay,
            DayOfWeek = fullDayExclusionDate.DayOfWeek, // Saturday
            StartTimeUtc = TimeSpan.Zero,
            EndTimeUtc = TimeSpan.FromDays(1), // Represents the full day
            ActiveFromUtc = fullDayExclusionDate, // Active only on this date
            ActiveUntilUtc = fullDayExclusionDate, // Active only on this date
        };

        // Time-based exclusion for a specific Friday afternoon (e.g., appointment)
        var timeBasedExclusionDate = new DateTime(2025, 6, 6, 0, 0, 0, DateTimeKind.Utc); // Friday, June 6th, 2025
        var timeBasedExclusionRule = new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Owner = _testOwner,
            OwnerType = ownerType,
            RuleType = AvailabilityRuleType.ExclusionTimeBased,
            DayOfWeek = timeBasedExclusionDate.DayOfWeek, // Friday
            StartTimeUtc = TimeSpan.FromHours(11), // 11:00 AM
            EndTimeUtc = TimeSpan.FromHours(12), // 12:00 PM
            ActiveFromUtc = timeBasedExclusionDate, // Active only on this date
            ActiveUntilUtc = timeBasedExclusionDate, // Active only on this date
        };

        // Standard availability rule with an end date (e.g., temporary change)
        var expiringRuleStartDate = new DateTime(2025, 6, 3, 0, 0, 0, DateTimeKind.Utc); // Tuesday, June 3rd, 2025
        var expiringRuleEndDate = new DateTime(2025, 6, 10, 0, 0, 0, DateTimeKind.Utc); // Tuesday, June 10th, 2025
        var expiringRule = new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Owner = _testOwner,
            OwnerType = ownerType,
            RuleType = AvailabilityRuleType.AvailabilityStandard,
            DayOfWeek = DayOfWeek.Tuesday, // Applies to Tuesdays
            StartTimeUtc = TimeSpan.FromHours(10), // 10:00 AM
            EndTimeUtc = TimeSpan.FromHours(12), // 12:00 PM
            ActiveFromUtc = expiringRuleStartDate, // Starts from this Tuesday
            ActiveUntilUtc = expiringRuleEndDate, // Ends on this Tuesday
        };

        await AvailabilityRulesRepository.AddRangeAsync(
            [recurringRule, oneOffRule, exclusionRule, timeBasedExclusionRule, expiringRule]
        );
        await AvailabilityRulesRepository.SaveChangesAsync();

        // Act
        var result = await _query.ExecuteAsync(ownerId, ownerType);

        // Assert
        Assert.True(result.IsSuccess);
        var ruleDtos = result.Value;

        // Expected: All 5 rules added should be returned as DTOs
        Assert.Equal(5, ruleDtos.Count);

        // Verify key properties of the DTOs match the added rules, checking RuleType and specific dates/times
        Assert.Contains(
            ruleDtos,
            dto =>
                dto.Id == recurringRule.Id
                && dto.DayOfWeek == DayOfWeek.Monday
                && dto.StartTimeUtc == TimeSpan.FromHours(9)
                && dto.EndTimeUtc == TimeSpan.FromHours(13)
                && dto.ActiveFromUtc.Date == startDate.Date
                && !dto.ActiveUntilUtc.HasValue
                && dto.RuleType == AvailabilityRuleType.AvailabilityStandard
        );
        Assert.Contains(
            ruleDtos,
            dto =>
                dto.Id == oneOffRule.Id
                && dto.DayOfWeek == DayOfWeek.Thursday
                && dto.StartTimeUtc == TimeSpan.FromHours(14)
                && dto.EndTimeUtc == TimeSpan.FromHours(16)
                && dto.ActiveFromUtc.Date == oneOffDate.Date
                && dto.ActiveUntilUtc!.Value.Date == oneOffDate.Date
                && dto.RuleType == AvailabilityRuleType.AvailabilityOneOff
        );
        Assert.Contains(
            ruleDtos,
            dto =>
                dto.Id == exclusionRule.Id
                && dto.DayOfWeek == DayOfWeek.Saturday
                && dto.StartTimeUtc == TimeSpan.Zero
                && dto.EndTimeUtc == TimeSpan.FromDays(1)
                && dto.ActiveFromUtc.Date == fullDayExclusionDate.Date
                && dto.ActiveUntilUtc!.Value.Date == fullDayExclusionDate.Date
                && dto.RuleType == AvailabilityRuleType.ExclusionFullDay
        );
        Assert.Contains(
            ruleDtos,
            dto =>
                dto.Id == timeBasedExclusionRule.Id
                && dto.DayOfWeek == DayOfWeek.Friday
                && dto.StartTimeUtc == TimeSpan.FromHours(11)
                && dto.EndTimeUtc == TimeSpan.FromHours(12)
                && dto.ActiveFromUtc.Date == timeBasedExclusionDate.Date
                && dto.ActiveUntilUtc!.Value.Date == timeBasedExclusionDate.Date
                && dto.RuleType == AvailabilityRuleType.ExclusionTimeBased
        );
        Assert.Contains(
            ruleDtos,
            dto =>
                dto.Id == expiringRule.Id
                && dto.DayOfWeek == DayOfWeek.Tuesday
                && dto.StartTimeUtc == TimeSpan.FromHours(10)
                && dto.EndTimeUtc == TimeSpan.FromHours(12)
                && dto.ActiveFromUtc.Date == expiringRuleStartDate.Date
                && dto.ActiveUntilUtc.HasValue
                && dto.ActiveUntilUtc.Value.Date == expiringRuleEndDate.Date
                && dto.RuleType == AvailabilityRuleType.AvailabilityStandard
        );
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnEmptyList_WhenOwnerHasNoRules()
    {
        // Scenario: Query for rules for an owner who has no rules configured.
        // Arrange
        var ownerId = _testOwner.Id; // Use the test owner, but don't add rules for them in this test
        var ownerType = AvailabilityOwnerType.Tutor; // Use a different type to ensure isolation

        // Act
        var result = await _query.ExecuteAsync(ownerId, ownerType);

        // Assert
        Assert.True(result.IsSuccess);
        var ruleDtos = result.Value;

        // Expected: Empty list
        Assert.Empty(ruleDtos);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnOnlyCorrectOwnersRules_WhenRulesExistForOtherOwnersAndTypes_RealWorld()
    {
        // Scenario: Query for rules for a specific owner/type, ensuring rules belonging to other owners or different types for the same owner are NOT returned, in a real-world context.
        // Arrange
        var startDate = new DateTime(2025, 7, 1, 0, 0, 0, DateTimeKind.Utc); // Tuesday, July 1st, 2025
        var testOwnerId = _testOwner.Id;
        var otherOwnerId = _otherOwner.Id;

        // Rules for the TARGET owner (Reviewer) - these should be returned
        // Target Standard Availability
        var targetRule1 = new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            OwnerId = testOwnerId,
            Owner = _testOwner,
            OwnerType = AvailabilityOwnerType.Reviewer,
            RuleType = AvailabilityRuleType.AvailabilityStandard,
            DayOfWeek = DayOfWeek.Wednesday, // Applies to Wednesdays
            StartTimeUtc = TimeSpan.FromHours(9),
            EndTimeUtc = TimeSpan.FromHours(11),
            ActiveFromUtc = startDate,
            ActiveUntilUtc = null,
        };
        // Target One-Off Availability
        var targetOneOffDate = new DateTime(2025, 7, 4, 0, 0, 0, DateTimeKind.Utc); // Friday, July 4th, 2025
        var targetRule2 = new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            OwnerId = testOwnerId,
            Owner = _testOwner,
            OwnerType = AvailabilityOwnerType.Reviewer,
            RuleType = AvailabilityRuleType.AvailabilityOneOff,
            DayOfWeek = targetOneOffDate.DayOfWeek, // Friday
            StartTimeUtc = TimeSpan.FromHours(15),
            EndTimeUtc = TimeSpan.FromHours(17),
            ActiveFromUtc = targetOneOffDate,
            ActiveUntilUtc = targetOneOffDate,
        };
        // Target Full-Day Exclusion
        var targetFullDayExclusionDate = new DateTime(2025, 7, 5, 0, 0, 0, DateTimeKind.Utc); // Saturday, July 5th, 2025
        var targetRule3 = new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            OwnerId = testOwnerId,
            Owner = _testOwner,
            OwnerType = AvailabilityOwnerType.Reviewer,
            RuleType = AvailabilityRuleType.ExclusionFullDay,
            DayOfWeek = targetFullDayExclusionDate.DayOfWeek, // Saturday
            StartTimeUtc = TimeSpan.Zero,
            EndTimeUtc = TimeSpan.FromDays(1),
            ActiveFromUtc = targetFullDayExclusionDate,
            ActiveUntilUtc = targetFullDayExclusionDate,
        };
        // Target Time-Based Exclusion
        var targetTimeBasedExclusionDate = new DateTime(2025, 7, 7, 0, 0, 0, DateTimeKind.Utc); // Monday, July 7th, 2025
        var targetRule4 = new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            OwnerId = testOwnerId,
            Owner = _testOwner,
            OwnerType = AvailabilityOwnerType.Reviewer,
            RuleType = AvailabilityRuleType.ExclusionTimeBased,
            DayOfWeek = targetTimeBasedExclusionDate.DayOfWeek, // Monday
            StartTimeUtc = TimeSpan.FromHours(10), // 10:00 AM
            EndTimeUtc = TimeSpan.FromHours(11), // 11:00 AM
            ActiveFromUtc = targetTimeBasedExclusionDate,
            ActiveUntilUtc = targetTimeBasedExclusionDate,
        };

        // Rules for the TARGET owner but DIFFERENT type (Tutor) - these should NOT be returned by the query for Reviewer
        var sameOwnerOtherTypeRule = new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            OwnerId = testOwnerId,
            Owner = _testOwner,
            OwnerType = AvailabilityOwnerType.Tutor, // Different OwnerType
            RuleType = AvailabilityRuleType.AvailabilityStandard,
            DayOfWeek = DayOfWeek.Tuesday, // Use a day different from target rules
            StartTimeUtc = TimeSpan.FromHours(16),
            EndTimeUtc = TimeSpan.FromHours(18),
            ActiveFromUtc = startDate,
            ActiveUntilUtc = null,
        };

        // Rules for a DIFFERENT owner (Reviewer) - these should NOT be returned
        var otherOwnerRule = new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            OwnerId = otherOwnerId, // Different OwnerId
            Owner = _otherOwner,
            OwnerType = AvailabilityOwnerType.Reviewer,
            RuleType = AvailabilityRuleType.AvailabilityStandard,
            DayOfWeek = DayOfWeek.Wednesday, // Same DayOfWeek as targetRule1 but different owner
            StartTimeUtc = TimeSpan.FromHours(8),
            EndTimeUtc = TimeSpan.FromHours(9),
            ActiveFromUtc = startDate,
            ActiveUntilUtc = null,
        };

        await AvailabilityRulesRepository.AddRangeAsync(
            [
                targetRule1,
                targetRule2,
                targetRule3,
                targetRule4,
                sameOwnerOtherTypeRule,
                otherOwnerRule,
            ]
        );
        await AvailabilityRulesRepository.SaveChangesAsync();

        // Act
        // Query for the TARGET owner and TARGET type (Reviewer)
        var result = await _query.ExecuteAsync(testOwnerId, AvailabilityOwnerType.Reviewer);

        // Assert
        Assert.True(result.IsSuccess);
        var ruleDtos = result.Value;

        // Expected: Only the four target rules should be returned
        Assert.Equal(4, ruleDtos.Count);

        // Verify the IDs match the expected rules
        Assert.Contains(ruleDtos, dto => dto.Id == targetRule1.Id);
        Assert.Contains(ruleDtos, dto => dto.Id == targetRule2.Id);
        Assert.Contains(ruleDtos, dto => dto.Id == targetRule3.Id);
        Assert.Contains(ruleDtos, dto => dto.Id == targetRule4.Id);

        // Explicitly verify the IDs of the other rules are NOT present
        Assert.DoesNotContain(ruleDtos, dto => dto.Id == sameOwnerOtherTypeRule.Id);
        Assert.DoesNotContain(ruleDtos, dto => dto.Id == otherOwnerRule.Id);
    }
}
