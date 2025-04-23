using Application.UseCases.Commands.TutorApplicationOnboarding.Applicant.Step3.BookInterview;
using Domain.Entities;
using Xcel.Services.Email.Templates;
using Xcel.TestUtils;

namespace Domain.IntegrationTests.UseCases.Queries.TutorApplicationOnboarding.Applicant.Step3;

public class ApplicantBookInterviewSlotCommandTests : BaseTest
{
    private IApplicantBookInterviewSlotCommand _command = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        _command = new ApplicantBookInterviewSlotCommand(TutorApplicationsRepository, AvailabilityRulesRepository, InMemoryEmailService, CreateLogger<ApplicantBookInterviewSlotCommand>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldBookSlot_WhenInputIsValid()
    {
        var now = FakeTimeProvider.GetUtcNow().UtcDateTime;

        var applicant = new Person { FirstName = "Tina", LastName = "Wells", EmailAddress = "tina@xcel.com" };
        var reviewer = new Person { FirstName = "Lucas", LastName = "Day", EmailAddress = "lucas@xcel.com" };

        var rule = new AvailabilityRule
        {
            Owner = reviewer,
            OwnerId = reviewer.Id,
            OwnerType = AvailabilityOwnerType.Reviewer,
            DayOfWeek = now.DayOfWeek,
            StartTimeUtc = new(13, 0, 0),
            EndTimeUtc = new(16, 0, 0),
            ActiveFromUtc = now.Date,
            ActiveUntilUtc = now.Date,
            IsExcluded = false
        };

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
        await AvailabilityRulesRepository.AddAsync(rule);
        await TutorApplicationsRepository.AddAsync(application);
        await TutorApplicationsRepository.SaveChangesAsync();

        var slot = now.Date.AddHours(13);
        var input = new ApplicantBookInterviewSlotInput(application.Id, slot, "Looking forward");

        // Act
        var result = await _command.ExecuteAsync(input);

        // Assert
        Assert.True(result.IsSuccess);

        var updatedApp = await TutorApplicationsRepository.GetByIdAsync(application.Id);
        Assert.Equal(TutorApplicationInterview.InterviewStatus.Confirmed, updatedApp!.Interview!.Status);
        Assert.Equal(slot, updatedApp.Interview.ScheduledAtUtc);
        Assert.Equal(applicant.Id, updatedApp.Interview.ConfirmedBy);

        var email = InMemoryEmailService.GetSentEmail<InterviewScheduledEmail>();
        Assert.NotNull(email);

        var payload = email.Payload.Data;
        Assert.Equal(applicant.FullName, payload.ApplicantFullName);
        Assert.Equal(reviewer.FullName, payload.ReviewerFullName);
        Assert.Equal(slot, payload.ScheduledAtUtc);

        var expectedEmail = new InterviewScheduledEmail(applicant.FullName, reviewer.FullName, slot);
        Assert.Equal(expectedEmail.Subject, email.Payload.Subject);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenInvalidSlot()
    {
        var applicant = new Person { FirstName = "Bob", LastName = "Invalid", EmailAddress = "bob@xcel.com" };
        var reviewer = new Person { FirstName = "Zoe", LastName = "NoSlot", EmailAddress = "zoe@xcel.com" };

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

        var slot = FakeTimeProvider.GetUtcNow().UtcDateTime.Date.AddHours(10);
        var input = new ApplicantBookInterviewSlotInput(application.Id, slot, "No valid slot");

        // Act
        var result = await _command.ExecuteAsync(input);

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(ApplicantBookInterviewSlotCommandErrors.InvalidSlot.Message, error.Message);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenInterviewNotInCorrectStatus()
    {
        var applicant = new Person { FirstName = "Alex", LastName = "WrongState", EmailAddress = "alex@xcel.com" };
        var reviewer = new Person { FirstName = "Dana", LastName = "StillWaiting", EmailAddress = "dana@xcel.com" };

        var application = new TutorApplication
        {
            Applicant = applicant,
            Interview = new()
            {
                Reviewer = reviewer,
                Status = TutorApplicationInterview.InterviewStatus.Confirmed
            }
        };

        await PersonsRepository.AddRangeAsync([applicant, reviewer]);
        await TutorApplicationsRepository.AddAsync(application);
        await TutorApplicationsRepository.SaveChangesAsync();

        var input = new ApplicantBookInterviewSlotInput(application.Id, FakeTimeProvider.GetUtcNow().UtcDateTime.Date.AddHours(14), null);

        var result = await _command.ExecuteAsync(input);

        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(ApplicantBookInterviewSlotCommandErrors.InterviewNotSelectable.Message, error.Message);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenInterviewMissing()
    {
        var applicant = new Person { FirstName = "Missing", LastName = "Interview", EmailAddress = "missing@xcel.com" };

        var application = new TutorApplication
        {
            Applicant = applicant,
            Interview = null!
        };

        await PersonsRepository.AddAsync(applicant);
        await TutorApplicationsRepository.AddAsync(application);
        await TutorApplicationsRepository.SaveChangesAsync();

        var input = new ApplicantBookInterviewSlotInput(application.Id, FakeTimeProvider.GetUtcNow().UtcDateTime, "none");
        var result = await _command.ExecuteAsync(input);

        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(ApplicantBookInterviewSlotCommandErrors.ApplicationOrInterviewNotFound.Message, error.Message);
    }
}
