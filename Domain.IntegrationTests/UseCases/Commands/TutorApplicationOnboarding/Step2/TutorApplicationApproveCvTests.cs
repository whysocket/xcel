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
    public async Task Handle_ApprovesPendingTutorApplicationAndSendsEmailsToApplicantAndReviewer()
    {
        // Arrange
        var reviewerRoleId = await AuthServiceSdk.CreateRoleAsync(UserRoles.Reviewer);
        var reviewer = new Person
        {
            Id = Guid.NewGuid(),
            FirstName = "Reviewer",
            LastName = "lastnam",
            EmailAddress = "reviewer.lastnam@test.com",
        };

        await PersonsRepository.AddAsync(reviewer);
        await AuthServiceSdk.AddRoleToPersonAsync(reviewer.Id, reviewerRoleId.Value.Id);

        var applicant = new Person
            { Id = Guid.NewGuid(), FirstName = "John", LastName = "Doe", EmailAddress = "john.doe@example.com" };
        await PersonsRepository.AddAsync(applicant);

        var tutorApplication = new TutorApplication
        {
            ApplicantId = applicant.Id,
            Applicant = applicant,
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
        Assert.Equal(reviewer.Id, interview.ReviewerId);
        Assert.NotNull(interview.Reviewer);
        Assert.Equal(reviewer.EmailAddress, interview.Reviewer.EmailAddress);

        // Assert applicant email was sent
        var expectedApplicantEmail = new ApplicantCvApprovalEmail(
            applicant.FullName,
            reviewer.FullName);
        var sentApplicantEmail = InMemoryEmailService.GetSentEmail<ApplicantCvApprovalEmail>();
        Assert.NotNull(sentApplicantEmail);
        Assert.Equal(expectedApplicantEmail.Subject, sentApplicantEmail.Payload.Subject);
        Assert.Equal(applicant.EmailAddress, sentApplicantEmail.Payload.To.First());
        Assert.Equal(expectedApplicantEmail.ApplicantFullName, sentApplicantEmail.Payload.Data.ApplicantFullName);
        Assert.Equal(expectedApplicantEmail.ReviewerFullName, sentApplicantEmail.Payload.Data.ReviewerFullName);

        // Assert reviewer email was sent
        var expectedReviewerEmail = new ApplicantAssignedToReviewerEmail(reviewer.FullName, applicant.FullName);
        var sentReviewerEmail = InMemoryEmailService.GetSentEmail<ApplicantAssignedToReviewerEmail>();
        Assert.NotNull(sentReviewerEmail);
        Assert.Equal(expectedReviewerEmail.Subject, sentReviewerEmail.Payload.Subject);
        Assert.Equal(reviewer.EmailAddress, sentReviewerEmail.Payload.To.First());
        Assert.Equal(expectedReviewerEmail.ApplicantFullName, sentApplicantEmail.Payload.Data.ApplicantFullName);
        Assert.Equal(expectedReviewerEmail.ReviewerFullName, sentApplicantEmail.Payload.Data.ReviewerFullName);
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
        Assert.Equal(
            new Error(ErrorType.NotFound, $"Tutor Application with ID '{command.TutorApplicationId}' not found."),
            result.Errors.Single());
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
        Assert.Equal(
            new Error(ErrorType.Validation,
                $"Tutor Application with ID '{command.TutorApplicationId}' is not in the CV review state."),
            result.Errors.Single());
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
        Assert.Equal(
            new Error(ErrorType.Conflict,
                $"Tutor Application with ID '{command.TutorApplicationId}' is already rejected."),
            result.Errors.Single());
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
        Assert.Equal(
            new Error(ErrorType.Validation,
                $"Tutor Application with ID '{command.TutorApplicationId}' has an incorrect number of submitted documents."),
            result.Errors.Single());
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
            Documents =
            [
                new()
                {
                    DocumentType = TutorDocument.TutorDocumentType.Cv,
                    Status = TutorDocument.TutorDocumentStatus.ResubmissionNeeded, DocumentPath = "path/to/cv.pdf"
                },
            ]
        };

        await TutorApplicationsRepository.AddAsync(tutorApplication);
        await TutorApplicationsRepository.SaveChangesAsync();

        var command = new TutorApplicationApproveCv.Command(tutorApplication.Id);

        // Act
        var result = await Sender.Send(command);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(
            new Error(ErrorType.Validation,
                $"Tutor Application with ID '{command.TutorApplicationId}' CV document is missing or not in pending state."),
            result.Errors.Single());
    }
}