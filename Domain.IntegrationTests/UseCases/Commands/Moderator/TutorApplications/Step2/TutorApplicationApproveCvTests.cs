using Application.UseCases.Commands.Moderator.TutorApplications.Step2;
using Domain.Entities;
using Domain.Results;
using Xcel.Services.Email.Templates.TutorApprovalEmail;
using Xcel.TestUtils;

namespace Domain.IntegrationTests.UseCases.Commands.Moderator.TutorApplications.Step2;

public class TutorApplicationApproveCvTests : BaseTest
{
    [Fact]
    public async Task Handle_ApprovesPendingTutorApplicationAndSendsEmail()
    {
        // Arrange
        var person = new Person { FirstName = "John", LastName = "Doe", EmailAddress = "john.doe@example.com" };
        var tutorApplication = new TutorApplication
        {
            Person = person,
            CurrentStep = TutorApplication.OnboardingStep.CvUnderReview,
            Documents =
            [
                new()
                {
                    DocumentType = TutorDocument.TutorDocumentType.Cv,
                    Status = TutorDocument.TutorDocumentStatus.Pending, DocumentPath = "path/to/cv.pdf"
                }
            ]
        };

        await TutorApplicationsRepository.AddAsync(tutorApplication);
        await TutorApplicationsRepository.SaveChangesAsync();

        var command = new TutorApplicationApproveCv.Command(tutorApplication.Id);

        // Act
        var result = await Sender.Send(command);

        // Assert
        Assert.True(result.IsSuccess);

        var updatedTutorApplication = await TutorApplicationsRepository.GetByIdAsync(tutorApplication.Id);
        Assert.NotNull(updatedTutorApplication);
        Assert.Equal(TutorApplication.OnboardingStep.AwaitingInterviewBooking, updatedTutorApplication.CurrentStep);

        var sentEmail = InMemoryEmailSender.GetSentEmail<TutorApprovalEmailData>();

        Assert.Equal("Your CV has been approved. Let’s book your interview", sentEmail.Payload.Subject);
        Assert.Equal(person.EmailAddress, sentEmail.Payload.To);
        Assert.Equal(person.FirstName, sentEmail.Payload.Data.FirstName);
        Assert.Equal(person.LastName, sentEmail.Payload.Data.LastName);
    }

    [Fact]
    public async Task Handle_ReturnsFailWhenTutorApplicationNotFound()
    {
        // Arrange
        var command = new TutorApplicationApproveCv.Command(Guid.NewGuid());

        // Act
        var result = await Sender.Send(command);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(new Error(ErrorType.NotFound, $"Tutor Application with ID '{command.TutorApplicationId}' not found."), result.Errors.Single());
    }

    [Fact]
    public async Task Handle_ReturnsFailWhenTutorApplicationIsNotPending()
    {
        // Arrange
        var person = new Person { FirstName = "John", LastName = "Doe", EmailAddress = "john.doe@example.com" };
        var tutorApplication = new TutorApplication
        {
            Person = person,
            CurrentStep = TutorApplication.OnboardingStep.AwaitingInterviewBooking,
            Documents =
            [
                new()
                {
                    DocumentType = TutorDocument.TutorDocumentType.Cv,
                    Status = TutorDocument.TutorDocumentStatus.Approved, DocumentPath = "path/to/cv.pdf"
                }
            ]
        };

        await TutorApplicationsRepository.AddAsync(tutorApplication);
        await TutorApplicationsRepository.SaveChangesAsync();

        var command = new TutorApplicationApproveCv.Command(tutorApplication.Id);

        // Act
        var result = await Sender.Send(command);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(new Error(ErrorType.Validation, $"Tutor Application with ID '{command.TutorApplicationId}' is not in the CV review state."), result.Errors.Single());
    }

    [Fact]
    public async Task Handle_ReturnsFailWhenTutorApplicationIsRejected()
    {
        // Arrange
        var person = new Person { FirstName = "John", LastName = "Doe", EmailAddress = "john.doe@example.com" };
        var tutorApplication = new TutorApplication
        {
            Person = person,
            IsRejected = true,
            CurrentStep = TutorApplication.OnboardingStep.CvUnderReview,
            Documents =
            [
                new()
                {
                    DocumentType = TutorDocument.TutorDocumentType.Cv,
                    Status = TutorDocument.TutorDocumentStatus.Pending, DocumentPath = "path/to/cv.pdf"
                }
            ]
        };

        await TutorApplicationsRepository.AddAsync(tutorApplication);
        await TutorApplicationsRepository.SaveChangesAsync();

        var command = new TutorApplicationApproveCv.Command(tutorApplication.Id);

        // Act
        var result = await Sender.Send(command);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(new Error(ErrorType.Conflict, $"Tutor Application with ID '{command.TutorApplicationId}' is already rejected."), result.Errors.Single());
    }

    [Fact]
    public async Task Handle_ReturnsFailWhenTutorApplicationHasIncorrectDocumentCount()
    {
        // Arrange
        var person = new Person { FirstName = "John", LastName = "Doe", EmailAddress = "john.doe@example.com" };
        var tutorApplication = new TutorApplication
        {
            Person = person,
            CurrentStep = TutorApplication.OnboardingStep.CvUnderReview,
            Documents =
            [
                new()
                {
                    DocumentType = TutorDocument.TutorDocumentType.Cv,
                    Status = TutorDocument.TutorDocumentStatus.Pending, DocumentPath = "path/to/cv.pdf"
                },
                new()
                {
                    DocumentType = TutorDocument.TutorDocumentType.Id,
                    Status = TutorDocument.TutorDocumentStatus.Pending, DocumentPath = "path/to/id.pdf"
                }
            ]
        };

        await TutorApplicationsRepository.AddAsync(tutorApplication);
        await TutorApplicationsRepository.SaveChangesAsync();

        var command = new TutorApplicationApproveCv.Command(tutorApplication.Id);

        // Act
        var result = await Sender.Send(command);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(new Error(ErrorType.Validation, $"Tutor Application with ID '{command.TutorApplicationId}' has an incorrect number of submitted documents."), result.Errors.Single());
    }

    [Fact]
    public async Task Handle_ReturnsFailWhenCvDocumentIsMissingOrNotPending()
    {
        // Arrange
        var person = new Person { FirstName = "John", LastName = "Doe", EmailAddress = "john.doe@example.com" };
        var tutorApplication = new TutorApplication
        {
            Person = person,
            CurrentStep = TutorApplication.OnboardingStep.CvUnderReview,
            Documents = [
                new() { DocumentType = TutorDocument.TutorDocumentType.Cv, Status = TutorDocument.TutorDocumentStatus.ResubmissionNeeded, DocumentPath = "path/to/cv.pdf" },
            ]
        };

        await TutorApplicationsRepository.AddAsync(tutorApplication);
        await TutorApplicationsRepository.SaveChangesAsync();

        var command = new TutorApplicationApproveCv.Command(tutorApplication.Id);

        // Act
        var result = await Sender.Send(command);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(new Error(ErrorType.Validation, $"Tutor Application with ID '{command.TutorApplicationId}' CV document is missing or not in pending state."), result.Errors.Single());
    }
}