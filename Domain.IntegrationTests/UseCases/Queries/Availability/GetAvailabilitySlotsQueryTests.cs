using Application.UseCases.Queries.Availability;
using Domain.Entities;
using Xcel.TestUtils;

namespace Domain.IntegrationTests.UseCases.Queries.Availability;

public class GetAvailabilitySlotsQueryTests : BaseTest
{
    private IGetAvailabilitySlotsQuery _query = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _query = new GetAvailabilitySlotsQuery(
            AvailabilityRulesRepository,
            CreateLogger<GetAvailabilitySlotsQuery>()
        );
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnCorrectSlots_WhenValidRulesExist()
    {
        // Arrange
        var person = new Person
        {
            FirstName = "Valid",
            LastName = "Slots",
            EmailAddress = "slots@xcel.com",
        };
        await PersonsRepository.AddAsync(person);

        var today = FakeTimeProvider.GetUtcNow().UtcDateTime.Date;
        var rule = new AvailabilityRule
        {
            OwnerId = person.Id,
            Owner = person,
            OwnerType = AvailabilityOwnerType.Reviewer,
            DayOfWeek = today.DayOfWeek,
            StartTimeUtc = new TimeSpan(10, 0, 0),
            EndTimeUtc = new TimeSpan(11, 0, 0),
            ActiveFromUtc = today,
            ActiveUntilUtc = today,
            IsExcluded = false,
        };

        await AvailabilityRulesRepository.AddAsync(rule);
        await AvailabilityRulesRepository.SaveChangesAsync();

        var input = new AvailabilitySlotsQueryInput(
            OwnerId: person.Id,
            OwnerType: AvailabilityOwnerType.Reviewer,
            FromUtc: today.AddHours(0),
            ToUtc: today.AddHours(23),
            SlotDuration: TimeSpan.FromMinutes(30)
        );

        // Act
        var result = await _query.ExecuteAsync(input);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count); // 10:00-10:30, 10:30-11:00
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnEmpty_WhenOutsideAvailabilityWindow()
    {
        // Arrange
        var person = new Person
        {
            FirstName = "No",
            LastName = "Window",
            EmailAddress = "window@xcel.com",
        };
        await PersonsRepository.AddAsync(person);

        var rule = new AvailabilityRule
        {
            OwnerId = person.Id,
            Owner = person,
            OwnerType = AvailabilityOwnerType.Reviewer,
            DayOfWeek = DayOfWeek.Monday,
            StartTimeUtc = new TimeSpan(9, 0, 0),
            EndTimeUtc = new TimeSpan(11, 0, 0),
            ActiveFromUtc = FakeTimeProvider.GetUtcNow().UtcDateTime.AddDays(-10),
            ActiveUntilUtc = FakeTimeProvider.GetUtcNow().UtcDateTime.AddDays(-5),
            IsExcluded = false,
        };

        await AvailabilityRulesRepository.AddAsync(rule);
        await AvailabilityRulesRepository.SaveChangesAsync();

        var input = new AvailabilitySlotsQueryInput(
            OwnerId: person.Id,
            OwnerType: AvailabilityOwnerType.Reviewer,
            FromUtc: FakeTimeProvider.GetUtcNow().UtcDateTime,
            ToUtc: FakeTimeProvider.GetUtcNow().UtcDateTime.AddDays(1),
            SlotDuration: TimeSpan.FromMinutes(30)
        );

        // Act
        var result = await _query.ExecuteAsync(input);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }
}
