using Application.UseCases.Queries.TutorApplicationOnboarding.Moderator.Common;
using Domain.Entities;
using Xcel.TestUtils;

namespace Domain.IntegrationTests.UseCases.Queries.TutorApplicationOnboarding.Moderator.Common;

public class GetApplicationByIdQueryTests : BaseTest
{
    private IGetApplicationByIdQuery _query = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        _query = new GetApplicationByIdQuery(
            TutorApplicationsRepository,
            CreateLogger<GetApplicationByIdQuery>()
        );
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnApplication_WhenExists()
    {
        // Arrange
        var tutorAppId = Guid.NewGuid();
        var applicant = new Person
        {
            FirstName = "Jane",
            LastName = "Doe",
            EmailAddress = "jane@example.com",
        };
        var application = new TutorApplication
        {
            Id = tutorAppId,
            Applicant = applicant,
            ApplicantId = applicant.Id,
            Documents = [],
            CurrentStep = TutorApplication.OnboardingStep.CvAnalysis,
        };

        await TutorApplicationsRepository.AddAsync(application);
        await TutorApplicationsRepository.SaveChangesAsync();

        // Act
        var result = await _query.ExecuteAsync(tutorAppId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(application, result.Value);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenApplicationNotFound()
    {
        // Arrange
        var tutorAppId = Guid.NewGuid();

        // Act
        var result = await _query.ExecuteAsync(tutorAppId);

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(GetApplicationByIdQueryErrors.NotFound(tutorAppId), error);
    }
}
