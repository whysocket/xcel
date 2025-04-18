using Application.UseCases.Commands.TutorApplicationOnboarding.Step2;
using Domain.Constants;
using Domain.Entities;
using Domain.Results;
using Xcel.Services.Email.Templates;
using Xcel.TestUtils;

namespace Domain.IntegrationTests.UseCases.Commands.TutorApplicationOnboarding.Step2;

public class TutorApplicationApproveCvTests : BaseTest
{
    [Fact]
    public async Task Handle_ApprovesPendingTutorApplicationAndSendsEmail()
    {
        // Arrange
        var reviewers = ReviewersConstants.ReviewersEmails.Select((r, i) => new Person
        {
            Id = Guid.NewGuid(),
            FirstName = $"firstname reviewer{i}",
            LastName = $"lastname reviewer{i}",
            EmailAddress = r,
        });
        
        await PersonsRepository.AddRangeAsync(reviewers);
        
        var person = new Person { FirstName = "John", LastName = "Doe", EmailAddress = "john.doe@example.com" };
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

        var command = new TutorApplicationApproveCv.Command(tutorApplication.Id);

        // Act
        var result = await Sender.Send(command);

        // Assert
        Assert.True(result.IsSuccess);

        var updatedTutorApplication = await TutorApplicationsRepository.GetByIdAsync(tutorApplication.Id);
        Assert.NotNull(updatedTutorApplication);
        Assert.Equal(TutorApplication.OnboardingStep.AwaitingInterviewBooking, updatedTutorApplication.CurrentStep);

        // Assert interview was created
        var interview = updatedTutorApplication.Interview;
        Assert.NotNull(interview);
        Assert.Equal(TutorApplicationInterview.InterviewStatus.AwaitingReviewerProposedDates, interview.Status);
        Assert.Equal(TutorApplicationInterview.InterviewPlatform.GoogleMeets, interview.Platform);
        Assert.Equal(updatedTutorApplication.Id, interview.TutorApplicationId);
        Assert.NotEqual(Guid.Empty, interview.ReviewerId);
        Assert.NotNull(interview.Reviewer);

        // Assert email was sent
        var expectedEmail = new TutorCvApprovalEmail(
            person.FullName);
        var sentEmail = InMemoryEmailService.GetSentEmail<TutorCvApprovalEmail>();
        Assert.Equal(expectedEmail.Subject, sentEmail.Payload.Subject);
        Assert.Equal(person.EmailAddress, sentEmail.Payload.To.First());
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
            Applicant = person,
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
            Applicant = person,
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
            Applicant = person,
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
            Applicant = person,
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