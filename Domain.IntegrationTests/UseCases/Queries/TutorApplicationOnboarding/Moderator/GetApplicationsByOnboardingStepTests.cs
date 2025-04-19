using Application.UseCases.Queries.TutorApplicationOnboarding.Moderator;
using Domain.Entities;
using Xcel.TestUtils;

namespace Domain.IntegrationTests.UseCases.Queries.TutorApplicationOnboarding.Moderator;

public class GetApplicationsByOnboardingStepTests : BaseTest
{
    [Fact]
    public async Task Handle_ReturnsExpectedApplications_WhenInCorrectStep()
    {
        // Arrange
        var applicant = new Person
        {
            FirstName = "Lily",
            LastName = "Stone",
            EmailAddress = "lily@example.com"
        };

        var document = new TutorDocument
        {
            DocumentType = TutorDocument.TutorDocumentType.Cv,
            DocumentPath = "cv/lily.pdf",
            Status = TutorDocument.TutorDocumentStatus.Pending,
            Version = 1
        };

        var tutorApplication = new TutorApplication
        {
            Applicant = applicant,
            CurrentStep = TutorApplication.OnboardingStep.CvUnderReview,
            Documents = [document]
        };

        await PersonsRepository.AddAsync(applicant);
        await TutorApplicationsRepository.AddAsync(tutorApplication);
        await TutorApplicationsRepository.SaveChangesAsync();

        // Act
        var query = new GetApplicationsByOnboardingStep.Query(TutorApplication.OnboardingStep.CvUnderReview);
        var result = await Sender.Send(query);

        // Assert
        Assert.True(result.IsSuccess);

        var response = result.Value;
        var tutor = Assert.Single(response.TutorsApplications);

        Assert.Equal(tutorApplication.Id, tutor.TutorApplicationId);
        Assert.Equal("Lily", tutor.Person.FirstName);
        Assert.Equal("Stone", tutor.Person.LastName);
        Assert.Equal("lily@example.com", tutor.Person.EmailAddress);

        var doc = Assert.Single(tutor.Documents);
        Assert.NotEqual(Guid.Empty, doc.DocumentId);
        Assert.Equal("Pending", doc.Status);
        Assert.Equal("Cv", doc.Type);
        Assert.Equal(1, doc.Version);
        Assert.Equal("lily.pdf", Path.GetFileName(doc.Path));
    }

    [Fact]
    public async Task Handle_ReturnsEmptyList_WhenNoApplicationsInStep()
    {
        // Arrange
        var query = new GetApplicationsByOnboardingStep.Query(TutorApplication.OnboardingStep.Onboarded);

        // Act
        var result = await Sender.Send(query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value.TutorsApplications);
    }
}