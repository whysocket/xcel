using Application.UseCases.Queries.TutorApplicationOnboarding.Moderator;
using Domain.Entities;
using Xcel.TestUtils;

namespace Domain.IntegrationTests.UseCases.Queries.TutorApplicationOnboarding.Moderator;

public class GetApplicationByIdTests : BaseTest
{
    [Fact]
    public async Task Handle_ReturnsExpectedTutorApplication_WhenExistsAndInCorrectStep()
    {
        // Arrange
        var applicant = new Person
        {
            FirstName = "Sam",
            LastName = "Jordan",
            EmailAddress = "sam@example.com"
        };

        var cvDocument = new TutorDocument
        {
            DocumentType = TutorDocument.TutorDocumentType.Cv,
            DocumentPath = "path/to/cv.pdf",
            Status = TutorDocument.TutorDocumentStatus.Pending,
            Version = 1,
            ModeratorReason = "Initial review"
        };

        var tutorApplication = new TutorApplication
        {
            Applicant = applicant,
            CurrentStep = TutorApplication.OnboardingStep.CvUnderReview,
            Documents = [cvDocument]
        };

        await PersonsRepository.AddAsync(applicant);
        await TutorApplicationsRepository.AddAsync(tutorApplication);
        await TutorApplicationsRepository.SaveChangesAsync();

        // Act
        var query = new GetApplicationById.Query(tutorApplication.Id);
        var result = await Sender.Send(query);

        // Assert
        Assert.True(result.IsSuccess);
        var response = result.Value;

        Assert.Equal("Sam", response.Person.FirstName);
        Assert.Equal("Jordan", response.Person.LastName);
        Assert.Equal("sam@example.com", response.Person.EmailAddress);

        var doc = Assert.Single(response.Documents);
        Assert.Equal("Pending", doc.Status);
        Assert.Equal("Cv", doc.Type);
        Assert.Equal("cv.pdf", Path.GetFileName(doc.Path));
        Assert.Equal(1, doc.Version);
        Assert.Equal("Initial review", doc.ModeratorReason);
    }

    [Fact]
    public async Task Handle_ReturnsFailure_WhenApplicationDoesNotExist()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var query = new GetApplicationById.Query(nonExistentId);
        var result = await Sender.Send(query);

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(GetApplicationById.Errors.NotFound.Message, error.Message);
    }
}