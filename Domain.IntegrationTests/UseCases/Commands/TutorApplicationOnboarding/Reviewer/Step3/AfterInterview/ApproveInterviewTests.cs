using Application.UseCases.Commands.TutorApplicationOnboarding.Reviewer.Step3.AfterInterview;
using Domain.Entities;
using Domain.Results;
using NSubstitute;
using Xcel.Services.Email.Interfaces;
using Xcel.Services.Email.Models;
using Xcel.Services.Email.Templates;
using Xcel.TestUtils;

namespace Domain.IntegrationTests.UseCases.Commands.TutorApplicationOnboarding.Reviewer.Step3.AfterInterview;

public class ApproveInterviewCommandTests : BaseTest
{
    private IApproveInterviewCommand _command = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        _command = new ApproveInterviewCommand(
            TutorApplicationsRepository,
            InMemoryEmailService,
            CreateLogger<ApproveInterviewCommand>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldAdvanceToDocumentsStep_AndSendEmail_WhenInterviewIsConfirmed()
    {
        // Arrange
        var applicant = new Person { FirstName = "Eva", LastName = "Green", EmailAddress = "eva@xcel.com" };
        var application = new TutorApplication
        {
            Applicant = applicant,
            Interview = new TutorApplicationInterview
            {
                Reviewer = new Person
                {
                    FirstName = "Olivia",
                    LastName = "Jones",
                    EmailAddress = "olivia@xcel.com"
                },
                Status = TutorApplicationInterview.InterviewStatus.Confirmed
            },
            CurrentStep = TutorApplication.OnboardingStep.InterviewBooking
        };

        await PersonsRepository.AddAsync(applicant);
        await TutorApplicationsRepository.AddAsync(application);
        await TutorApplicationsRepository.SaveChangesAsync();

        // Act
        var result = await _command.ExecuteAsync(application.Id);

        // Assert
        Assert.True(result.IsSuccess);
        var updated = await TutorApplicationsRepository.GetByIdAsync(application.Id);
        Assert.Equal(TutorApplication.OnboardingStep.DocumentsAnalysis, updated!.CurrentStep);

        var expectedEmail = new TutorRequestDocumentsEmail(applicant.FullName);
        var sentEmail = InMemoryEmailService.GetSentEmail<TutorRequestDocumentsEmail>();
        Assert.NotNull(sentEmail);
        Assert.Equal(expectedEmail.Subject, sentEmail.Payload.Subject);
        Assert.Equal(applicant.EmailAddress, sentEmail.Payload.To.First());
        Assert.Equal(expectedEmail.ApplicantFullName, sentEmail.Payload.Data.ApplicantFullName);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenInterviewIsNotConfirmed()
    {
        // Arrange
        var applicant = new Person { FirstName = "Paul", LastName = "Smith", EmailAddress = "paul@xcel.com" };
        var application = new TutorApplication
        {
            Applicant = applicant,
            Interview = new TutorApplicationInterview
            {
                Reviewer = new Person
                {
                    FirstName = "Olivia",
                    LastName = "Jones",
                    EmailAddress = "olivia@xcel.com"
                },
                Status = TutorApplicationInterview.InterviewStatus.AwaitingApplicantSlotSelection
            },
            CurrentStep = TutorApplication.OnboardingStep.InterviewBooking
        };

        await PersonsRepository.AddAsync(applicant);
        await TutorApplicationsRepository.AddAsync(application);
        await TutorApplicationsRepository.SaveChangesAsync();

        // Act
        var result = await _command.ExecuteAsync(application.Id);

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(ApproveInterviewCommandErrors.InvalidInterviewState(application.Id), error);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenEmailFails()
    {
        // Arrange
        var applicant = new Person { FirstName = "James", LastName = "Was", EmailAddress = "james@xcel.com" };
        var application = new TutorApplication
        {
            Applicant = applicant,
            Interview = new TutorApplicationInterview
            {
                Reviewer = new Person
                {
                    FirstName = "Olivia",
                    LastName = "Jones",
                    EmailAddress = "olivia@xcel.com"
                },
                Status = TutorApplicationInterview.InterviewStatus.Confirmed
            },
            CurrentStep = TutorApplication.OnboardingStep.InterviewBooking
        };

        await PersonsRepository.AddAsync(applicant);
        await TutorApplicationsRepository.AddAsync(application);
        await TutorApplicationsRepository.SaveChangesAsync();

        var failEmailService = Substitute.For<IEmailService>();
        failEmailService.SendEmailAsync(Arg.Any<EmailPayload<TutorRequestDocumentsEmail>>(),
                Arg.Any<CancellationToken>())
            .Returns(Result.Fail(ApproveInterviewCommandErrors.EmailSendFailed(application.Id)));

        // Act
        var result = await new ApproveInterviewCommand(
                TutorApplicationsRepository,
                failEmailService,
                CreateLogger<ApproveInterviewCommand>())
            .ExecuteAsync(application.Id);

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(ApproveInterviewCommandErrors.EmailSendFailed(application.Id), error);
    }
}