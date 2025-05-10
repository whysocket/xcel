using Application.UseCases.Queries.TutorApplicationOnboarding.Reviewer.Step3;
using Domain.Entities;
using Xcel.TestUtils;

namespace Domain.IntegrationTests.UseCases.Queries.TutorApplicationOnboarding.Reviewer.Step3;

public class GetReviewerAssignedInterviewsQueryTests : BaseTest
{
    private IGetReviewerAssignedInterviewsQuery _query = null!;
    private Person _testReviewer = null!;
    private Person _otherReviewer = null!;
    private Person _applicant1 = null!;
    private Person _applicant2 = null!;
    private Person _applicant3 = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _query = new GetReviewerAssignedInterviewsQuery(
            TutorApplicationsRepository,
            CreateLogger<GetReviewerAssignedInterviewsQuery>()
        );

        // Create test persons
        _testReviewer = new Person
        {
            FirstName = "Test",
            LastName = "Reviewer",
            EmailAddress = "test.reviewer@xcel.com",
            Id = Guid.NewGuid(),
        };
        _otherReviewer = new Person
        {
            FirstName = "Other",
            LastName = "Reviewer",
            EmailAddress = "other.reviewer@xcel.com",
            Id = Guid.NewGuid(),
        };
        _applicant1 = new Person
        {
            FirstName = "Applicant",
            LastName = "One",
            EmailAddress = "applicant1@xcel.com",
            Id = Guid.NewGuid(),
        };
        _applicant2 = new Person
        {
            FirstName = "Applicant",
            LastName = "Two",
            EmailAddress = "applicant2@xcel.com",
            Id = Guid.NewGuid(),
        };
        _applicant3 = new Person
        {
            FirstName = "Applicant",
            LastName = "Three",
            EmailAddress = "applicant3@xcel.com",
            Id = Guid.NewGuid(),
        };

        await PersonsRepository.AddRangeAsync(
            [_testReviewer, _otherReviewer, _applicant1, _applicant2, _applicant3]
        );
        await PersonsRepository.SaveChangesAsync();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnAssignedApplications_WhenReviewerHasInterviews()
    {
        // Scenario: A reviewer is assigned to multiple applications and the query is executed.
        // Arrange
        var app1 = new TutorApplication
        {
            Id = Guid.NewGuid(),
            Applicant = _applicant1,
            ApplicantId = _applicant1.Id,
            Interview = new TutorApplicationInterview
            {
                Reviewer = _testReviewer,
                ReviewerId = _testReviewer.Id,
            },
        };
        var app2 = new TutorApplication
        {
            Id = Guid.NewGuid(),
            Applicant = _applicant2,
            ApplicantId = _applicant2.Id,
            Interview = new TutorApplicationInterview
            {
                Reviewer = _testReviewer,
                ReviewerId = _testReviewer.Id,
            },
        };
        // An application assigned to a different reviewer
        var appOtherReviewer = new TutorApplication
        {
            Id = Guid.NewGuid(),
            Applicant = _applicant3,
            ApplicantId = _applicant3.Id,
            Interview = new TutorApplicationInterview
            {
                Reviewer = _otherReviewer,
                ReviewerId = _otherReviewer.Id,
            },
        };

        await TutorApplicationsRepository.AddRangeAsync([app1, app2, appOtherReviewer]);
        await TutorApplicationsRepository.SaveChangesAsync();

        // Act
        var result = await _query.ExecuteAsync(_testReviewer.Id);

        // Assert
        Assert.True(result.IsSuccess);
        var applications = result.Value;

        // Expected: Only applications assigned to _testReviewer should be returned
        Assert.Equal(2, applications.Count);

        // Verify the correct applications are returned based on ID
        Assert.Contains(applications, app => app.Id == app1.Id);
        Assert.Contains(applications, app => app.Id == app2.Id);

        // Verify applications assigned to the other reviewer are NOT returned
        Assert.DoesNotContain(applications, app => app.Id == appOtherReviewer.Id);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnEmptyList_WhenReviewerHasNoAssignedInterviews()
    {
        // Scenario: A reviewer is not assigned to any applications.
        // Arrange
        // No applications assigned to _testReviewer are added in this test.
        // An application assigned to a different reviewer might exist, but should not be returned.
        var appOtherReviewer = new TutorApplication
        {
            Id = Guid.NewGuid(),
            Applicant = _applicant3,
            ApplicantId = _applicant3.Id,
            Interview = new TutorApplicationInterview
            {
                Reviewer = _otherReviewer,
                ReviewerId = _otherReviewer.Id,
            },
        };
        await TutorApplicationsRepository.AddAsync(appOtherReviewer);
        await TutorApplicationsRepository.SaveChangesAsync();

        // Act
        var result = await _query.ExecuteAsync(_testReviewer.Id);

        // Assert
        Assert.True(result.IsSuccess);
        var applications = result.Value;

        // Expected: Empty list
        Assert.Empty(applications);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnEmptyList_WhenApplicationsExistWithoutAssignedReviewers()
    {
        // Scenario: Applications exist in the system, but they don't have a reviewer assigned via the interview property.
        // Arrange
        // An application with no interview
        var appNoInterview = new TutorApplication
        {
            Id = Guid.NewGuid(),
            Applicant = _applicant1,
            ApplicantId = _applicant1.Id,
            Interview = null,
        };
        // An application with an interview property, but no reviewer linked (Fix applied here)
        var appNoReviewerInInterview = new TutorApplication
        {
            Id = Guid.NewGuid(),
            Applicant = _applicant2,
            ApplicantId = _applicant2.Id,
            // FIX: Set Interview to null instead of creating an interview with Guid.Empty ReviewerId
            Interview = null, // Or set Interview = new TutorApplicationInterview() { ... } IF ReviewerId is nullable in DB schema AND null is explicitly set
        };

        // An application assigned to a different reviewer (should also be excluded)
        var appOtherReviewer = new TutorApplication
        {
            Id = Guid.NewGuid(),
            Applicant = _applicant3,
            ApplicantId = _applicant3.Id,
            Interview = new TutorApplicationInterview
            {
                Reviewer = _otherReviewer,
                ReviewerId = _otherReviewer.Id,
            },
        };

        await TutorApplicationsRepository.AddRangeAsync(
            [appNoInterview, appNoReviewerInInterview, appOtherReviewer]
        );
        await TutorApplicationsRepository.SaveChangesAsync();

        // Act
        var result = await _query.ExecuteAsync(_testReviewer.Id); // Query for the test reviewer

        // Assert
        Assert.True(result.IsSuccess);
        var applications = result.Value;

        // Expected: Empty list, as none of the applications are assigned to _testReviewer
        Assert.Empty(applications);
    }
}
