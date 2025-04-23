using Application.UseCases.Commands.TutorApplicationOnboarding.Moderator.Step2;
using Domain.Entities;
using Domain.Results;
using NSubstitute;
using Xcel.Services.Auth.Public;
using Xcel.Services.Email.Interfaces;
using Xcel.Services.Email.Models;
using Xcel.Services.Email.Templates;
using Xcel.TestUtils;

namespace Domain.IntegrationTests.UseCases.Commands.TutorApplicationOnboarding.Moderator.Step2;

public class ApplicationRejectCvCommandTests : BaseTest
{
    private IApplicationRejectCvCommand _command = null!;
    private IAuthServiceSdk _authService = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _authService = Substitute.For<IAuthServiceSdk>();

        _command = new ApplicationRejectCvCommand(
            TutorApplicationsRepository,
            _authService,
            InMemoryEmailService,
            CreateLogger<ApplicationRejectCvCommand>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRejectCv_WhenValid()
    {
        // Arrange
        var applicant = new Person { FirstName = "Emma", LastName = "Stone", EmailAddress = "emma@xcel.com" };
        var application = new TutorApplication
        {
            Applicant = applicant,
            Documents = [ new TutorDocument { DocumentType = TutorDocument.TutorDocumentType.Cv, Status = TutorDocument.TutorDocumentStatus.Pending, DocumentPath = "fake_path" } ],
            CurrentStep = TutorApplication.OnboardingStep.CvAnalysis
        };
        await PersonsRepository.AddAsync(applicant);
        await TutorApplicationsRepository.AddAsync(application);
        await TutorApplicationsRepository.SaveChangesAsync();

        _authService.DeleteAccountAsync(applicant.Id, Arg.Any<CancellationToken>()).Returns(Result.Ok());

        // Act
        var result = await _command.ExecuteAsync(application.Id, "Missing experience");

        // Assert
        Assert.True(result.IsSuccess);

        var updated = await TutorApplicationsRepository.GetByIdAsync(application.Id);
        Assert.True(updated!.IsRejected);

        var expectedEmail = new ApplicantCvRejectionEmail(applicant.FullName, "Missing experience");
        var sentEmail = InMemoryEmailService.GetSentEmail<ApplicantCvRejectionEmail>();
        Assert.NotNull(sentEmail);
        Assert.Equal(expectedEmail.Subject, sentEmail.Payload.Subject);
        Assert.Equal(applicant.EmailAddress, sentEmail.Payload.To.First());
        Assert.Equal(expectedEmail.ApplicantFullName, sentEmail.Payload.Data.ApplicantFullName);
        Assert.Equal(expectedEmail.RejectionReason, sentEmail.Payload.Data.RejectionReason);
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
        Assert.Equal(ApplicationRejectCvCommandErrors.TutorApplicationNotFound(fakeId), error);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenEmailFails()
    {
        // Arrange
        var applicant = new Person { FirstName = "No", LastName = "Email", EmailAddress = "noemail@xcel.com" };
        var application = new TutorApplication
        {
            Applicant = applicant,
            Documents = [ new TutorDocument { DocumentType = TutorDocument.TutorDocumentType.Cv, Status = TutorDocument.TutorDocumentStatus.Pending, DocumentPath = "fake_path" } ],
            CurrentStep = TutorApplication.OnboardingStep.CvAnalysis
        };
        await PersonsRepository.AddAsync(applicant);
        await TutorApplicationsRepository.AddAsync(application);
        await TutorApplicationsRepository.SaveChangesAsync();

        var failingEmailService = Substitute.For<IEmailService>();
        failingEmailService.SendEmailAsync(Arg.Any<EmailPayload<ApplicantCvRejectionEmail>>(), Arg.Any<CancellationToken>())
            .Returns(Result.Fail(ApplicationRejectCvCommandErrors.EmailSendFailed(applicant.EmailAddress)));

        var command = new ApplicationRejectCvCommand(
            TutorApplicationsRepository,
            _authService,
            failingEmailService,
            CreateLogger<ApplicationRejectCvCommand>());

        // Act
        var result = await command.ExecuteAsync(application.Id);

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(ApplicationRejectCvCommandErrors.EmailSendFailed(applicant.EmailAddress), error);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenAccountDeletionFails()
    {
        // Arrange
        var applicant = new Person { FirstName = "No", LastName = "Delete", EmailAddress = "nodelete@xcel.com" };
        var application = new TutorApplication
        {
            Applicant = applicant,
            Documents = [ new TutorDocument { DocumentType = TutorDocument.TutorDocumentType.Cv, Status = TutorDocument.TutorDocumentStatus.Pending, DocumentPath = "fake_path" } ],
            CurrentStep = TutorApplication.OnboardingStep.CvAnalysis
        };
        await PersonsRepository.AddAsync(applicant);
        await TutorApplicationsRepository.AddAsync(application);
        await TutorApplicationsRepository.SaveChangesAsync();

        _authService.DeleteAccountAsync(applicant.Id, Arg.Any<CancellationToken>())
            .Returns(Result.Fail(ApplicationRejectCvCommandErrors.AccountDeletionFailed(applicant.Id)));

        // Act
        var result = await _command.ExecuteAsync(application.Id);

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(ApplicationRejectCvCommandErrors.AccountDeletionFailed(applicant.Id), error);
    }
}