using Application.UseCases.Queries.Availability;
using Application.UseCases.Queries.TutorApplicationOnboarding.Applicant.Step3;
using Domain.Entities;
using Domain.Results;
using NSubstitute;
using Xcel.TestUtils;

namespace Domain.IntegrationTests.UseCases.Commands.TutorApplicationOnboarding.Applicant.Step3;

public class GetReviewerAvailabilitySlotsQueryTests : BaseTest
{
    private IGetReviewerAvailabilitySlotsQuery _query = null!;
    private IGetAvailabilitySlotsQuery _availabilityQuery = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _availabilityQuery = Substitute.For<IGetAvailabilitySlotsQuery>();
        _query = new GetReviewerAvailabilitySlotsQuery(
            TutorApplicationsRepository,
            _availabilityQuery,
            FakeTimeProvider,
            CreateLogger<GetReviewerAvailabilitySlotsQuery>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnSlots_WhenValid()
    {
        var now = FakeTimeProvider.GetUtcNow().UtcDateTime;
        var today = DateOnly.FromDateTime(now);

        var applicant = new Person { FirstName = "Alice", LastName = "Smith", EmailAddress = "alice@example.com" };
        var reviewer = new Person { FirstName = "John", LastName = "AfterInterview", EmailAddress = "john@example.com" };
        var application = new TutorApplication
        {
            Applicant = applicant,
            Interview = new TutorApplicationInterview { Reviewer = reviewer }
        };

        await PersonsRepository.AddRangeAsync([applicant, reviewer]);
        await TutorApplicationsRepository.AddAsync(application);
        await TutorApplicationsRepository.SaveChangesAsync();

        var expectedSlots = new List<AvailableSlot> { new(now.AddHours(13), now.AddHours(13.5)) };
        _availabilityQuery.ExecuteAsync(Arg.Any<AvailabilitySlotsQueryInput>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok(expectedSlots));

        var result = await _query.ExecuteAsync(application.Id, today);

        Assert.True(result.IsSuccess);
        var slot = Assert.Single(result.Value);
        Assert.Equal(expectedSlots[0].StartUtc, slot.StartUtc);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenNotOwner()
    {
        // Arrange
        var applicant = new Person { FirstName = "Fake", LastName = "User", EmailAddress = "fake@xcel.com" };
        var realApplicant = new Person { FirstName = "Real", LastName = "User", EmailAddress = "real@xcel.com" };
        var reviewer = new Person { FirstName = "Dan", LastName = "AfterInterview", EmailAddress = "dan@xcel.com" };

        var application = new TutorApplication
        {
            Applicant = realApplicant,
            Interview = new() { Reviewer = reviewer }
        };

        await PersonsRepository.AddRangeAsync([applicant, realApplicant, reviewer]);
        await TutorApplicationsRepository.AddAsync(application);
        await TutorApplicationsRepository.SaveChangesAsync();

        // Act
        var result = await _query.ExecuteAsync(applicant.Id, DateOnly.FromDateTime(FakeTimeProvider.GetUtcNow().UtcDateTime));

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(GetReviewerAvailabilitySlotsQueryErrors.Unauthorized(applicant.Id, application.Id), error);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenNoReviewerAssigned()
    {
        var applicant = new Person { FirstName = "Jake", LastName = "NoReviewer", EmailAddress = "jake@xcel.com" };
        var application = new TutorApplication { Applicant = applicant, Interview = null! };

        await PersonsRepository.AddAsync(applicant);
        await TutorApplicationsRepository.AddAsync(application);
        await TutorApplicationsRepository.SaveChangesAsync();

        var result = await _query.ExecuteAsync(applicant.Id, DateOnly.FromDateTime(FakeTimeProvider.GetUtcNow().UtcDateTime));

        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(GetReviewerAvailabilitySlotsQueryErrors.ReviewerNotAssigned(application.Id), error);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenAvailabilityQueryFails()
    {
        var now = FakeTimeProvider.GetUtcNow().UtcDateTime;
        var today = DateOnly.FromDateTime(now);

        var applicant = new Person { FirstName = "Fail", LastName = "Query", EmailAddress = "fail@xcel.com" };
        var reviewer = new Person { FirstName = "Down", LastName = "Service", EmailAddress = "down@xcel.com" };
        var application = new TutorApplication { Applicant = applicant, Interview = new() { Reviewer = reviewer } };

        await PersonsRepository.AddRangeAsync([applicant, reviewer]);
        await TutorApplicationsRepository.AddAsync(application);
        await TutorApplicationsRepository.SaveChangesAsync();

        var error = new Error(ErrorType.Unexpected, "System failure");
        _availabilityQuery.ExecuteAsync(Arg.Any<AvailabilitySlotsQueryInput>(), Arg.Any<CancellationToken>())
            .Returns(Result.Fail<List<AvailableSlot>>(error));

        var result = await _query.ExecuteAsync(applicant.Id, today);

        Assert.True(result.IsFailure);
        Assert.Contains(error, result.Errors);
    }
}
