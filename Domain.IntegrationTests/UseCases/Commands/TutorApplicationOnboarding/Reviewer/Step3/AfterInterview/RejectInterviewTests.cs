using Application.UseCases.Commands.TutorApplicationOnboarding.Reviewer.Step3.AfterInterview;
using Domain.Entities;
using Domain.Results;
using NSubstitute;
using Xcel.Services.Auth.Public;
using Xcel.Services.Email.Interfaces;
using Xcel.Services.Email.Models;
using Xcel.Services.Email.Templates;
using Xcel.TestUtils;

namespace Domain.IntegrationTests.UseCases.Commands.TutorApplicationOnboarding.Reviewer.Step3.AfterInterview;

public class RejectInterviewCommandTests : BaseTest
{
    private IRejectInterviewCommand _command = null!;
    private IAuthServiceSdk _authService = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _authService = Substitute.For<IAuthServiceSdk>();
        _command = new RejectInterviewCommand(
            TutorApplicationsRepository,
            _authService,
            InMemoryEmailService,
            CreateLogger<RejectInterviewCommand>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRejectConfirmedInterview()
    {
        var applicant = new Person { FirstName = "John", LastName = "Doe", EmailAddress = "john@xcel.com" };
        var reviewer = new Person { FirstName = "Anna", LastName = "Smith", EmailAddress = "anna@xcel.com" };
        var application = new TutorApplication
        {
            Applicant = applicant,
            Interview = new TutorApplicationInterview
            {
                Reviewer = reviewer,
                Status = TutorApplicationInterview.InterviewStatus.Confirmed
            },
            CurrentStep = TutorApplication.OnboardingStep.InterviewBooking
        };

        await PersonsRepository.AddRangeAsync([applicant, reviewer]);
        await TutorApplicationsRepository.AddAsync(application);
        await TutorApplicationsRepository.SaveChangesAsync();

        _authService.DeleteAccountAsync(applicant.Id, Arg.Any<CancellationToken>())
            .Returns(Result.Ok());

        var result = await _command.ExecuteAsync(application.Id, "Did not meet expectations");

        Assert.True(result.IsSuccess);

        var updated = await TutorApplicationsRepository.GetByIdAsync(application.Id);
        Assert.True(updated!.IsRejected);

        var expectedEmail = new TutorInterviewRejectionEmail(applicant.FullName, "Did not meet expectations");
        var sentEmail = InMemoryEmailService.GetSentEmail<TutorInterviewRejectionEmail>();
        Assert.NotNull(sentEmail);
        Assert.Equal(expectedEmail.Subject, sentEmail.Payload.Subject);
        Assert.Equal(applicant.EmailAddress, sentEmail.Payload.To.First());
        Assert.Equal(expectedEmail.ApplicantFullName, sentEmail.Payload.Data.ApplicantFullName);
        Assert.Equal(expectedEmail.RejectionReason, sentEmail.Payload.Data.RejectionReason);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenApplicationNotFound()
    {
        var nonExistingId = Guid.NewGuid();
        var result = await _command.ExecuteAsync(nonExistingId);

        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(RejectInterviewCommandErrors.TutorApplicationNotFound(nonExistingId), error);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenInterviewNotConfirmed()
    {
        var applicant = new Person { FirstName = "Not", LastName = "Confirmed", EmailAddress = "not@xcel.com" };
        var reviewer = new Person { FirstName = "Anna", LastName = "Smith", EmailAddress = "anna@xcel.com" };
        var application = new TutorApplication
        {
            Applicant = applicant,
            Interview = new TutorApplicationInterview
            {
                Reviewer = reviewer,
                Status = TutorApplicationInterview.InterviewStatus.AwaitingApplicantSlotSelection
            }
        };

        await PersonsRepository.AddRangeAsync([applicant, reviewer]);
        await TutorApplicationsRepository.AddAsync(application);
        await TutorApplicationsRepository.SaveChangesAsync();

        var result = await _command.ExecuteAsync(application.Id);

        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(RejectInterviewCommandErrors.InvalidInterviewState, error);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenEmailFails()
    {
        var emailService = Substitute.For<IEmailService>();
        var applicant = new Person { FirstName = "Fail", LastName = "Email", EmailAddress = "fail@xcel.com" };
        var reviewer = new Person { FirstName = "Anna", LastName = "Smith", EmailAddress = "anna@xcel.com" };
        var application = new TutorApplication
        {
            Applicant = applicant,
            Interview = new TutorApplicationInterview
            {
                Reviewer = reviewer,
                Status = TutorApplicationInterview.InterviewStatus.Confirmed
            }
        };

        await PersonsRepository.AddRangeAsync([applicant, reviewer]);
        await TutorApplicationsRepository.AddAsync(application);
        await TutorApplicationsRepository.SaveChangesAsync();

        emailService.SendEmailAsync(Arg.Any<EmailPayload<TutorInterviewRejectionEmail>>(), Arg.Any<CancellationToken>())
            .Returns(Result.Fail(RejectInterviewCommandErrors.EmailSendFailed(applicant.EmailAddress)));

        var command = new RejectInterviewCommand(
            TutorApplicationsRepository,
            _authService,
            emailService,
            CreateLogger<RejectInterviewCommand>());

        var result = await command.ExecuteAsync(application.Id);

        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(RejectInterviewCommandErrors.EmailSendFailed(applicant.EmailAddress), error);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenAccountDeletionFails()
    {
        var applicant = new Person { FirstName = "No", LastName = "Delete", EmailAddress = "nodelete@xcel.com" };
        var reviewer = new Person { FirstName = "Anna", LastName = "Smith", EmailAddress = "anna@xcel.com" };
        var application = new TutorApplication
        {
            Applicant = applicant,
            Interview = new TutorApplicationInterview
            {
                Reviewer = reviewer,
                Status = TutorApplicationInterview.InterviewStatus.Confirmed
            }
        };

        await PersonsRepository.AddRangeAsync([applicant, reviewer]);
        await TutorApplicationsRepository.AddAsync(application);
        await TutorApplicationsRepository.SaveChangesAsync();

        _authService.DeleteAccountAsync(applicant.Id, Arg.Any<CancellationToken>())
            .Returns(Result.Fail(RejectInterviewCommandErrors.AccountDeletionFailed(applicant.Id)));

        var result = await _command.ExecuteAsync(application.Id);

        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(RejectInterviewCommandErrors.AccountDeletionFailed(applicant.Id), error);
    }
}