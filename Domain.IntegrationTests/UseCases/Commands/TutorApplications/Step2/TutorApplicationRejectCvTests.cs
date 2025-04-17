using Application.UseCases.Commands.TutorApplications.Step2;
using Domain.Entities;
using Domain.Results;
using Xcel.Services.Email.Templates;
using Xcel.TestUtils;

namespace Domain.IntegrationTests.UseCases.Commands.TutorApplications.Step2;

public class TutorApplicationRejectCvTests : BaseTest
{
    [Fact]
    public async Task Handle_RejectsPendingTutorApplicationAndSendsEmailAndDeleteAccount()
    {
        // Arrange
        var person = new Person { FirstName = "Jane", LastName = "Smith", EmailAddress = "jane.smith@example.com" };
        var tutorApplication = new TutorApplication
        {
            Applicant = person,
            CurrentStep = TutorApplication.OnboardingStep.CvUnderReview,
            Documents =
            [
                new()
                {
                    DocumentType = TutorDocument.TutorDocumentType.Cv,
                    Status = TutorDocument.TutorDocumentStatus.Pending,
                    DocumentPath = "path/to/cv.pdf"
                }
            ]
        };

        await TutorApplicationsRepository.AddAsync(tutorApplication);
        await TutorApplicationsRepository.SaveChangesAsync();

        var rejectionReason = "Insufficient qualifications.";
        var command = new TutorApplicationRejectCv.Command(tutorApplication.Id, rejectionReason);

        // Act
        var result = await Sender.Send(command);

        // Assert
        Assert.True(result.IsSuccess);

        var updatedTutorApplication = await TutorApplicationsRepository.GetByIdAsync(tutorApplication.Id);
        Assert.NotNull(updatedTutorApplication);
        Assert.True(updatedTutorApplication.IsRejected);

        var sentEmail = InMemoryEmailService.GetSentEmail<TutorCvRejectionEmail>();
        Assert.Equal("Your application was rejected", sentEmail.Payload.Subject);
        Assert.Equal(person.EmailAddress, sentEmail.Payload.To.First());
        Assert.Equal(person.FullName, sentEmail.Payload.Data.FullName);
        Assert.Equal(rejectionReason, sentEmail.Payload.Data.RejectionReason);

        Assert.Null(await PersonsRepository.GetByIdAsync(person.Id));
        Assert.NotNull(await PersonsRepository.GetDeletedByIdAsync(person.Id));
    }

    [Fact]
    public async Task Handle_ReturnsFailureWhenTutorApplicationNotFound()
    {
        // Arrange
        var command = new TutorApplicationRejectCv.Command(Guid.NewGuid(), "Reason");

        // Act
        var result = await Sender.Send(command);

        // Assert
        Assert.Single(result.Errors);
        Assert.True(result.IsFailure);
        Assert.Equal(new Error(ErrorType.NotFound, $"Tutor Application with ID '{command.TutorApplicationId}' not found."), result.Errors.Single());
    }

    [Fact]
    public async Task Handle_ReturnsFailureWhenTutorApplicationIsNotInCvReviewStep()
    {
        // Arrange
        var person = new Person { FirstName = "Jane", LastName = "Smith", EmailAddress = "jane.smith@example.com" };
        var tutorApplication = new TutorApplication
        {
            Applicant = person,
            CurrentStep = TutorApplication.OnboardingStep.AwaitingInterviewBooking,
            Documents =
            [
                new()
                {
                    DocumentType = TutorDocument.TutorDocumentType.Cv,
                    Status = TutorDocument.TutorDocumentStatus.Approved,
                    DocumentPath = "path/to/cv.pdf"
                }
            ]
        };

        await TutorApplicationsRepository.AddAsync(tutorApplication);
        await TutorApplicationsRepository.SaveChangesAsync();

        var command = new TutorApplicationRejectCv.Command(tutorApplication.Id, "Reason");

        // Act
        var result = await Sender.Send(command);

        // Assert
        Assert.Single(result.Errors);
        Assert.True(result.IsFailure);
        Assert.Equal(new Error(ErrorType.Validation, $"Tutor Application with ID '{command.TutorApplicationId}' is not in the CV review state."), result.Errors.Single());
    }

    [Fact]
    public async Task Handle_ReturnsFailureWhenTutorApplicationIsAlreadyRejected()
    {
        // Arrange
        var person = new Person { FirstName = "Jane", LastName = "Smith", EmailAddress = "jane.smith@example.com" };
        var tutorApplication = new TutorApplication
        {
            Applicant = person,
            IsRejected = true,
            CurrentStep = TutorApplication.OnboardingStep.CvUnderReview,
            Documents =
            [
                new()
                {
                    DocumentType = TutorDocument.TutorDocumentType.Cv,
                    Status = TutorDocument.TutorDocumentStatus.Pending,
                    DocumentPath = "path/to/cv.pdf"
                }
            ]
        };

        await TutorApplicationsRepository.AddAsync(tutorApplication);
        await TutorApplicationsRepository.SaveChangesAsync();

        var command = new TutorApplicationRejectCv.Command(tutorApplication.Id, "Reason");

        // Act
        var result = await Sender.Send(command);

        // Assert
        Assert.Single(result.Errors);
        Assert.True(result.IsFailure);
        Assert.Equal(new Error(ErrorType.Conflict, $"Tutor Application with ID '{command.TutorApplicationId}' is already rejected."), result.Errors.Single());
    }

    [Fact]
    public async Task Handle_ReturnsFailureWhenTutorApplicationHasIncorrectDocumentCount()
    {
        // Arrange
        var person = new Person { FirstName = "Jane", LastName = "Smith", EmailAddress = "jane.smith@example.com" };
        var tutorApplication = new TutorApplication
        {
            Applicant = person,
            CurrentStep = TutorApplication.OnboardingStep.CvUnderReview,
            Documents =
            [
                new()
                {
                    DocumentType = TutorDocument.TutorDocumentType.Cv,
                    Status = TutorDocument.TutorDocumentStatus.Pending,
                    DocumentPath = "path/to/cv.pdf"
                },
                new()
                {
                    DocumentType = TutorDocument.TutorDocumentType.Id,
                    Status = TutorDocument.TutorDocumentStatus.Pending,
                    DocumentPath = "path/to/id.pdf"
                }
            ]
        };

        await TutorApplicationsRepository.AddAsync(tutorApplication);
        await TutorApplicationsRepository.SaveChangesAsync();

        var command = new TutorApplicationRejectCv.Command(tutorApplication.Id, "Reason");

        // Act
        var result = await Sender.Send(command);

        // Assert
        Assert.Single(result.Errors);
        Assert.True(result.IsFailure);
        Assert.Equal(new Error(ErrorType.Validation, $"Tutor Application with ID '{command.TutorApplicationId}' has an incorrect number of submitted documents."), result.Errors.Single());
    }
}