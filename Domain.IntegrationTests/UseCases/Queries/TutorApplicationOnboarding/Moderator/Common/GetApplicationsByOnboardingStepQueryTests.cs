using Application.UseCases.Queries.TutorApplicationOnboarding.Moderator.Common;
using Domain.Entities;
using Xcel.TestUtils;

namespace Domain.IntegrationTests.UseCases.Queries.TutorApplicationOnboarding.Moderator.Common;

public class GetApplicationsByOnboardingStepQueryTests : BaseTest
{
    private IGetApplicationsByOnboardingStepQuery _query = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _query = new GetApplicationsByOnboardingStepQuery(TutorApplicationsRepository, CreateLogger<GetApplicationsByOnboardingStepQuery>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnApplications_WhenStepMatches()
    {
        // Arrange
        var applicant = new Person { FirstName = "John", LastName = "Doe", EmailAddress = "john@xcel.com" };
        var application = new TutorApplication
        {
            Applicant = applicant,
            Documents =
            [
                new()
                {
                    DocumentType = TutorDocument.TutorDocumentType.Cv,
                    Status = TutorDocument.TutorDocumentStatus.Pending,
                    DocumentPath = "path/cv.pdf"
                }
            ],
            CurrentStep = TutorApplication.OnboardingStep.CvAnalysis
        };

        await PersonsRepository.AddAsync(applicant);
        await TutorApplicationsRepository.AddAsync(application);
        await TutorApplicationsRepository.SaveChangesAsync();

        // Act
        var result = await _query.ExecuteAsync(TutorApplication.OnboardingStep.CvAnalysis);

        // Assert
        Assert.True(result.IsSuccess);
        var returned = Assert.Single(result.Value);
        Assert.Equal(application.Id, returned.Id);
        Assert.Equal(applicant.EmailAddress, returned.Applicant.EmailAddress);
        Assert.Single(returned.Documents);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnEmpty_WhenNoApplicationsMatch()
    {
        // Act
        var result = await _query.ExecuteAsync(TutorApplication.OnboardingStep.DocumentsAnalysis);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }
}