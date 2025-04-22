using Application.UseCases.Commands.TutorApplicationOnboarding.Applicant.Step3.BookInterview.Reviewer;
using Domain.Entities;
using Xcel.Services.Email.Templates;
using Xcel.TestUtils;

namespace Domain.IntegrationTests.UseCases.Commands.TutorApplicationOnboarding.Reviewer.Step3.BeforeInterview;

public class ReviewerRequestInterviewRescheduleCommandTests : BaseTest
{
    private IReviewerRequestInterviewRescheduleCommand _command = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _command = new ReviewerRequestInterviewRescheduleCommand(TutorApplicationsRepository, InMemoryEmailService, CreateLogger<ReviewerRequestInterviewRescheduleCommand>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldResetInterviewAndSendEmail_WhenConfirmed()
    {
        var applicant = new Person { FirstName = "Chris", LastName = "Evans", EmailAddress = "chris@app.com" };
        var reviewer = new Person { FirstName = "Jane", LastName = "Smith", EmailAddress = "jane@xcel.com" };

        var application = new TutorApplication
        {
            Applicant = applicant,
            Interview = new()
            {
                Reviewer = reviewer,
                Status = TutorApplicationInterview.InterviewStatus.Confirmed,
                ScheduledAtUtc = FakeTimeProvider.GetUtcNow().UtcDateTime
            }
        };

        await PersonsRepository.AddRangeAsync([applicant, reviewer]);
        await TutorApplicationsRepository.AddAsync(application);
        await TutorApplicationsRepository.SaveChangesAsync();

        var reason = "Need to shift due to calendar conflict";
        var input = new ReviewerRequestInterviewRescheduleInput(application.Id, reason);

        // Act
        var result = await _command.ExecuteAsync(input);

        // Assert
        Assert.True(result.IsSuccess);

        var updatedApp = await TutorApplicationsRepository.GetByIdAsync(application.Id);
        Assert.Equal(TutorApplicationInterview.InterviewStatus.AwaitingApplicantSlotSelection, updatedApp!.Interview!.Status);
        Assert.Null(updatedApp.Interview.ScheduledAtUtc);
        Assert.Equal(reason, updatedApp.Interview.Observations);

        var email = InMemoryEmailService.GetSentEmail<ReviewerRescheduleRequestEmail>();
        Assert.NotNull(email);

        var payload = email.Payload.Data;
        Assert.Equal(applicant.FullName, payload.ApplicantFullName);
        Assert.Equal(reviewer.FullName, payload.ReviewerFullName);
        Assert.Equal(reason, payload.RescheduleReason);

        var expectedEmail = new ReviewerRescheduleRequestEmail(
            applicant.FullName,
            reviewer.FullName,
            reason);

        Assert.Equal(expectedEmail.Subject, email.Payload.Subject);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenInterviewIsNotConfirmed()
    {
        var applicant = new Person { FirstName = "Lily", LastName = "Rose", EmailAddress = "lily@xcel.com" };
        var reviewer = new Person { FirstName = "Alex", LastName = "Brown", EmailAddress = "alex@xcel.com" };

        var application = new TutorApplication
        {
            Applicant = applicant,
            Interview = new()
            {
                Reviewer = reviewer,
                Status = TutorApplicationInterview.InterviewStatus.AwaitingApplicantSlotSelection
            }
        };

        await PersonsRepository.AddRangeAsync([applicant, reviewer]);
        await TutorApplicationsRepository.AddAsync(application);
        await TutorApplicationsRepository.SaveChangesAsync();

        var input = new ReviewerRequestInterviewRescheduleInput(application.Id, "doesn't matter");

        var result = await _command.ExecuteAsync(input);

        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(ReviewerRequestInterviewRescheduleCommandErrors.InterviewNotConfirmed.Message, error.Message);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenInterviewIsMissing()
    {
        var applicant = new Person { FirstName = "Tony", LastName = "Stark", EmailAddress = "tony@xcel.com" };

        var application = new TutorApplication { Applicant = applicant, Interview = null! };

        await PersonsRepository.AddAsync(applicant);
        await TutorApplicationsRepository.AddAsync(application);
        await TutorApplicationsRepository.SaveChangesAsync();

        var input = new ReviewerRequestInterviewRescheduleInput(application.Id, "reschedule plz");
        var result = await _command.ExecuteAsync(input);

        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(ReviewerRequestInterviewRescheduleCommandErrors.ApplicationOrInterviewNotFound.Message, error.Message);
    }
}
