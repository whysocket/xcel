using Application.UseCases.Queries.TutorApplicationOnboarding.Reviewer.Step3;
using Domain.Entities;
using Xcel.TestUtils;

namespace Domain.IntegrationTests.UseCases.Queries.TutorApplicationOnboarding.Reviewer.Step3;

public class GetReviewerAssignedInterviewsQueryTests : BaseTest
{
    private IGetReviewerAssignedInterviewsQuery _query = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _query = new GetReviewerAssignedInterviewsQuery(
            TutorApplicationsRepository,
            CreateLogger<GetReviewerAssignedInterviewsQuery>()
        );
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnAssignedInterviews_WhenReviewerIsAssigned()
    {
        // Arrange
        var reviewer = new Person
        {
            FirstName = "Eva",
            LastName = "Reviewer",
            EmailAddress = "eva@xcel.com",
        };
        var applicant = new Person
        {
            FirstName = "Nina",
            LastName = "Applicant",
            EmailAddress = "nina@xcel.com",
        };
        var application = new TutorApplication
        {
            Applicant = applicant,
            Interview = new TutorApplicationInterview
            {
                Reviewer = reviewer,
                Status = TutorApplicationInterview.InterviewStatus.Confirmed,
                ScheduledAtUtc = FakeTimeProvider.GetUtcNow().UtcDateTime.AddDays(1),
            },
        };

        await PersonsRepository.AddRangeAsync([reviewer, applicant]);
        await TutorApplicationsRepository.AddAsync(application);
        await TutorApplicationsRepository.SaveChangesAsync();

        // Act
        var result = await _query.ExecuteAsync(reviewer.Id);

        // Assert
        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Value);
        Assert.Equal(application.Id, item.Id);
        Assert.Equal(applicant.FullName, item.Applicant.FullName);
        Assert.Equal(application.Interview.ScheduledAtUtc, item.Interview!.ScheduledAtUtc);
        Assert.Equal(application.Interview.Status, item.Interview.Status);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnEmpty_WhenNoInterviewsAssigned()
    {
        // Arrange
        var reviewer = new Person
        {
            FirstName = "No",
            LastName = "Assignments",
            EmailAddress = "none@xcel.com",
        };
        await PersonsRepository.AddAsync(reviewer);
        await PersonsRepository.SaveChangesAsync();

        // Act
        var result = await _query.ExecuteAsync(reviewer.Id);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }
}
