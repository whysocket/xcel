using Application.UseCases.Queries.Availability;
using Domain.Entities;
using Xcel.TestUtils;

namespace Domain.IntegrationTests.UseCases.Queries.Availability;

public class GetAvailabilityRulesQueryTests : BaseTest
{
    private IGetAvailabilityRulesQuery _query = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _query = new GetAvailabilityRulesQuery(AvailabilityRulesRepository, CreateLogger<GetAvailabilityRulesQuery>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnRules_WhenRulesExist()
    {
        // Arrange
        var person = new Person { FirstName = "Ana", LastName = "Dev", EmailAddress = "ana@dev.com" };
        await PersonsRepository.AddAsync(person);

        var rule1 = new AvailabilityRule
        {
            OwnerId = person.Id,
            Owner = person,
            OwnerType = AvailabilityOwnerType.Reviewer,
            DayOfWeek = DayOfWeek.Wednesday,
            StartTimeUtc = new TimeSpan(14, 0, 0),
            EndTimeUtc = new TimeSpan(16, 0, 0),
            ActiveFromUtc = FakeTimeProvider.GetUtcNow().UtcDateTime.Date,
            ActiveUntilUtc = null,
            IsExcluded = false
        };

        var rule2 = new AvailabilityRule
        {
            OwnerId = person.Id,
            Owner = person,
            OwnerType = AvailabilityOwnerType.Reviewer,
            DayOfWeek = DayOfWeek.Friday,
            StartTimeUtc = new TimeSpan(10, 0, 0),
            EndTimeUtc = new TimeSpan(12, 0, 0),
            ActiveFromUtc = FakeTimeProvider.GetUtcNow().UtcDateTime.Date,
            ActiveUntilUtc = FakeTimeProvider.GetUtcNow().UtcDateTime.AddDays(30),
            IsExcluded = true
        };

        await AvailabilityRulesRepository.AddRangeAsync([rule1, rule2]);
        await AvailabilityRulesRepository.SaveChangesAsync();

        // Act
        var result = await _query.ExecuteAsync(person.Id, AvailabilityOwnerType.Reviewer);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);

        var wednesdayRule = result.Value.First(r => r.DayOfWeek == DayOfWeek.Wednesday);
        Assert.Equal(new TimeSpan(14, 0, 0), wednesdayRule.StartTimeUtc);
        Assert.False(wednesdayRule.IsExcluded);

        var fridayRule = result.Value.First(r => r.DayOfWeek == DayOfWeek.Friday);
        Assert.Equal(new TimeSpan(10, 0, 0), fridayRule.StartTimeUtc);
        Assert.True(fridayRule.IsExcluded);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnEmptyList_WhenNoRulesExist()
    {
        // Arrange
        var person = new Person { FirstName = "No", LastName = "Rules", EmailAddress = "none@dev.com" };
        await PersonsRepository.AddAsync(person);
        await AvailabilityRulesRepository.SaveChangesAsync();

        // Act
        var result = await _query.ExecuteAsync(person.Id, AvailabilityOwnerType.Reviewer);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }
} 