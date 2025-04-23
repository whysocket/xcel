using Application.Interfaces;
using Application.UseCases.Commands.TutorApplicationOnboarding.Moderator.Step2;
using Domain.Entities;
using Domain.Results;
using NSubstitute;
using Xcel.Services.Email.Interfaces;
using Xcel.Services.Email.Models;
using Xcel.Services.Email.Templates;
using Xcel.TestUtils;

namespace Domain.IntegrationTests.UseCases.Commands.TutorApplicationOnboarding.Moderator.Step2;

public class ApplicationApproveCvCommandTests : BaseTest
{
    private IApplicationApproveCvCommand _command = null!;
    private IReviewerAssignmentService _reviewerAssignmentService = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        _reviewerAssignmentService = Substitute.For<IReviewerAssignmentService>();

        _command = new ApplicationApproveCvCommand(
            TutorApplicationsRepository,
            _reviewerAssignmentService,
            InMemoryEmailService,
            CreateLogger<ApplicationApproveCvCommand>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldApproveCvAndSendEmails_WhenValid()
    {
        // Arrange
        var applicant = new Person { FirstName = "Sarah", LastName = "Lee", EmailAddress = "sarah@xcel.com" };
        var reviewer = new Person { FirstName = "Ben", LastName = "Stone", EmailAddress = "ben@xcel.com" };
        var application = new TutorApplication
        {
            Applicant = applicant,
            Documents =
            [
                new TutorDocument
                {
                    DocumentType = TutorDocument.TutorDocumentType.Cv,
                    Status = TutorDocument.TutorDocumentStatus.Pending,
                    DocumentPath = "fake_path"
                }
            ],
            CurrentStep = TutorApplication.OnboardingStep.CvAnalysis
        };
        await PersonsRepository.AddRangeAsync([applicant, reviewer]);
        await TutorApplicationsRepository.AddAsync(application);
        await TutorApplicationsRepository.SaveChangesAsync();

        _reviewerAssignmentService.GetAvailableReviewerAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok(reviewer));

        // Act
        var result = await _command.ExecuteAsync(application.Id);

        // Assert
        Assert.True(result.IsSuccess);

        var updated = await TutorApplicationsRepository.GetByIdAsync(application.Id);
        Assert.NotNull(updated!.Interview);
        Assert.Equal(reviewer.Id, updated.Interview.ReviewerId);
        Assert.Equal(TutorApplication.OnboardingStep.InterviewBooking, updated.CurrentStep);

        var expectedApplicantEmail = new ApplicantCvApprovalEmail(applicant.FullName, reviewer.FullName);
        var sentApplicantEmail = InMemoryEmailService.GetSentEmail<ApplicantCvApprovalEmail>();
        Assert.NotNull(sentApplicantEmail);
        Assert.Equal(expectedApplicantEmail.Subject, sentApplicantEmail.Payload.Subject);
        Assert.Equal(applicant.EmailAddress, sentApplicantEmail.Payload.To.First());
        Assert.Equal(expectedApplicantEmail.ApplicantFullName, sentApplicantEmail.Payload.Data.ApplicantFullName);
        Assert.Equal(expectedApplicantEmail.ReviewerFullName, sentApplicantEmail.Payload.Data.ReviewerFullName);

        var expectedReviewerEmail = new ApplicantAssignedToReviewerEmail(reviewer.FullName, applicant.FullName);
        var sentReviewerEmail = InMemoryEmailService.GetSentEmail<ApplicantAssignedToReviewerEmail>();
        Assert.NotNull(sentReviewerEmail);
        Assert.Equal(expectedReviewerEmail.Subject, sentReviewerEmail.Payload.Subject);
        Assert.Equal(reviewer.EmailAddress, sentReviewerEmail.Payload.To.First());
        Assert.Equal(expectedReviewerEmail.ApplicantFullName, sentReviewerEmail.Payload.Data.ApplicantFullName);
        Assert.Equal(expectedReviewerEmail.ReviewerFullName, sentReviewerEmail.Payload.Data.ReviewerFullName);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenApplicationNotFound()
    {
        // Arrange
        var fakeId = Guid.NewGuid();

        // Act
        var result = await _command.ExecuteAsync(fakeId);

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(ApplicationApproveCvCommandErrors.TutorApplicationNotFound(fakeId), error);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenEmailSendingFails()
    {
        // Arrange
        var applicant = new Person { FirstName = "Terry", LastName = "Fail", EmailAddress = "terry.fail@xcel.com" };
        var reviewer = new Person { FirstName = "Lena", LastName = "Approve", EmailAddress = "lena.approve@xcel.com" };
        var application = new TutorApplication
        {
            Applicant = applicant,
            Documents =
            [
                new TutorDocument
                {
                    DocumentType = TutorDocument.TutorDocumentType.Cv,
                    Status = TutorDocument.TutorDocumentStatus.Pending,
                    DocumentPath = "cv_path"
                }
            ],
            CurrentStep = TutorApplication.OnboardingStep.CvAnalysis
        };

        await PersonsRepository.AddRangeAsync([applicant, reviewer]);
        await TutorApplicationsRepository.AddAsync(application);
        await TutorApplicationsRepository.SaveChangesAsync();

        _reviewerAssignmentService.GetAvailableReviewerAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Ok(reviewer));

        var failingEmailService = Substitute.For<IEmailService>();
        failingEmailService
            .SendEmailAsync(Arg.Any<EmailPayload<ApplicantCvApprovalEmail>>(), Arg.Any<CancellationToken>())
            .Returns(Result.Fail(new Error(ErrorType.Unexpected, "SMTP failure")));

        var command = new ApplicationApproveCvCommand(
            TutorApplicationsRepository,
            _reviewerAssignmentService,
            failingEmailService,
            CreateLogger<ApplicationApproveCvCommand>());

        // Act
        var result = await command.ExecuteAsync(application.Id);

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(ApplicationApproveCvCommandErrors.EmailSendFailed(application.Id), error);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenNoReviewerAvailable()
    {
        // Arrange
        var applicant = new Person { FirstName = "Alex", LastName = "Brown", EmailAddress = "alex@xcel.com" };
        var application = new TutorApplication
        {
            Applicant = applicant,
            Documents =
            [
                new TutorDocument
                {
                    DocumentType = TutorDocument.TutorDocumentType.Cv,
                    Status = TutorDocument.TutorDocumentStatus.Pending,
                    DocumentPath = "fake_path"
                }
            ],
            CurrentStep = TutorApplication.OnboardingStep.CvAnalysis
        };
        await PersonsRepository.AddAsync(applicant);
        await TutorApplicationsRepository.AddAsync(application);
        await TutorApplicationsRepository.SaveChangesAsync();

        var mockError = new Error(ErrorType.Validation, "No reviewers available.");
        _reviewerAssignmentService.GetAvailableReviewerAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Fail<Person>(mockError));

        // Act
        var result = await _command.ExecuteAsync(application.Id);

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(mockError, error);
    }
}