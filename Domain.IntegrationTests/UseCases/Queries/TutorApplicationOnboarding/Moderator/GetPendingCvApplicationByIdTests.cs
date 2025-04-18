using Application.UseCases.Queries.TutorApplicationOnboarding.Moderator;
using Domain.Entities;
using Xcel.TestUtils;

namespace Domain.IntegrationTests.UseCases.Queries.TutorApplicationOnboarding.Moderator;

public class GetPendingCvApplicationByIdTests : BaseTest
{
    [Fact]
    public async Task Handle_ReturnsExpectedTutorApplication_WhenExists()
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
            Version = 1
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
        var query = new GetPendingCvApplicationById.Query(tutorApplication.Id);
        var result = await Sender.Send(query);

        // Assert
        Assert.True(result.IsSuccess);

        var response = result.Value;
        Assert.Equal("Sam", response.Person.FirstName);
        Assert.Equal("Jordan", response.Person.LastName);
        Assert.Equal("sam@example.com", response.Person.EmailAddress);

        Assert.NotNull(response.CvDocument);
        Assert.Equal("Pending", response.CvDocument.Status);
        Assert.Equal("cv.pdf", Path.GetFileName(response.CvDocument.Path));
        Assert.Equal(1, response.CvDocument.Version);
    }

    [Fact]
    public async Task Handle_ReturnsFailure_WhenApplicationDoesNotExist()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var query = new GetPendingCvApplicationById.Query(nonExistentId);
        var result = await Sender.Send(query);

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(GetPendingCvApplicationById.Errors.NotFound.Message, error.Message);
    }

    [Fact]
    public async Task Handle_ReturnsFailure_WhenApplicationNotInCvUnderReview()
    {
        // Arrange
        var application = new TutorApplication
        {
            Applicant = new Person { FirstName = "Nia", LastName = "Cole", EmailAddress = "nia@example.com" },
            CurrentStep = TutorApplication.OnboardingStep.InterviewScheduled
        };

        await TutorApplicationsRepository.AddAsync(application);
        await TutorApplicationsRepository.SaveChangesAsync();

        // Act
        var query = new GetPendingCvApplicationById.Query(application.Id);
        var result = await Sender.Send(query);

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(GetPendingCvApplicationById.Errors.InvalidState.Message, error.Message);
    }
}