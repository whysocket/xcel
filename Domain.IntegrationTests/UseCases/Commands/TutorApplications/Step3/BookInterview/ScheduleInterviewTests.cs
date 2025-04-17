using Application.UseCases.Commands.TutorApplications.Step3.BookInterview;
using Domain.Entities;
using Xcel.Services.Email.Templates;
using Xcel.TestUtils;

namespace Domain.IntegrationTests.UseCases.Commands.TutorApplications.Step3.BookInterview;

public class ScheduleInterviewTests : BaseTest
{
    [Fact]
    public async Task Handle_SchedulesInterviewSuccessfully_AsReviewer()
    {
        // Arrange
        var reviewer = new Person { FirstName = "Eve", LastName = "Sun", EmailAddress = "reviewer@example.com" };
        var applicant = new Person { FirstName = "Tom", LastName = "Smith", EmailAddress = "tom@example.com" };
        var proposedDate = FakeTimeProvider.GetUtcNow().AddDays(3).UtcDateTime;

        var tutorApplication = new TutorApplication
        {
            Applicant = applicant,
            CurrentStep = TutorApplication.OnboardingStep.InterviewScheduled,
            Interview = new()
            {
                Reviewer = reviewer,
                Status = TutorApplicationInterview.InterviewStatus.AwaitingReviewerConfirmation,
                ProposedDates = [proposedDate]
            }
        };

        await PersonsRepository.AddRangeAsync([reviewer, applicant]);
        await TutorApplicationsRepository.AddAsync(tutorApplication);
        await TutorApplicationsRepository.SaveChangesAsync();

        var command = new ScheduleInterview.Command(tutorApplication.Id, proposedDate, ScheduleInterview.Party.Reviewer);

        // Act
        var result = await Sender.Send(command);

        // Assert
        Assert.True(result.IsSuccess);

        var updatedApplication = await TutorApplicationsRepository.GetByIdAsync(tutorApplication.Id);
        Assert.NotNull(updatedApplication);
        Assert.Equal(TutorApplicationInterview.InterviewStatus.Confirmed, updatedApplication.Interview!.Status);
        Assert.Equal(proposedDate, updatedApplication.Interview.ScheduledAt);

        var sentEmail = InMemoryEmailService.GetSentEmail<InterviewScheduledEmail>();
        Assert.Equal(applicant.EmailAddress, sentEmail.Payload.To.First());
        Assert.Equal(reviewer.EmailAddress, sentEmail.Payload.To.Last());
    }

    [Fact]
    public async Task Handle_ReturnsFailure_WhenSelectedDateInvalid()
    {
        var reviewer = new Person { FirstName = "Eva", LastName = "Stone", EmailAddress = "eva@example.com" };
        var tutorApplication = new TutorApplication
        {
            Applicant = new() { FirstName = "Bob", LastName = "Jet", EmailAddress = "bob@example.com" },
            Interview = new()
            {
                Reviewer = reviewer,
                Status = TutorApplicationInterview.InterviewStatus.AwaitingReviewerConfirmation,
                ProposedDates = [FakeTimeProvider.GetUtcNow().AddDays(2).UtcDateTime]
            }
        };

        await PersonsRepository.AddAsync(reviewer);
        await TutorApplicationsRepository.AddAsync(tutorApplication);
        await TutorApplicationsRepository.SaveChangesAsync();

        var invalidDate = FakeTimeProvider.GetUtcNow().AddDays(10).UtcDateTime;
        var command = new ScheduleInterview.Command(tutorApplication.Id, invalidDate, ScheduleInterview.Party.Reviewer);

        var result = await Sender.Send(command);

        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(ScheduleInterview.Errors.Handler.SelectedDateNotValid.Message, error.Message);
    }

    [Fact]
    public async Task Handle_ReturnsFailure_WhenInterviewStatusIsNotConfirmable()
    {
        var reviewer = new Person { FirstName = "Eli", LastName = "Bright", EmailAddress = "eli@example.com" };
        var tutorApplication = new TutorApplication
        {
            Applicant = new() { FirstName = "Jen", LastName = "Doe", EmailAddress = "jen@example.com" },
            Interview = new()
            {
                Reviewer = reviewer,
                Status = TutorApplicationInterview.InterviewStatus.Confirmed, // not confirmable
                ProposedDates = [FakeTimeProvider.GetUtcNow().AddDays(2).UtcDateTime]
            }
        };

        await PersonsRepository.AddAsync(reviewer);
        await TutorApplicationsRepository.AddAsync(tutorApplication);
        await TutorApplicationsRepository.SaveChangesAsync();

        var date = tutorApplication.Interview.ProposedDates.First();
        var command = new ScheduleInterview.Command(tutorApplication.Id, date, ScheduleInterview.Party.Reviewer);

        var result = await Sender.Send(command);

        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(ScheduleInterview.Errors.Handler.InterviewNotInCorrectState.Message, error.Message);
    }

    [Fact]
    public async Task Handle_ReturnsFailure_WhenPartyIsIncorrect()
    {
        var reviewer = new Person { FirstName = "Lee", LastName = "Night", EmailAddress = "lee@example.com" };
        var tutorApplication = new TutorApplication
        {
            Applicant = new() { FirstName = "Ron", LastName = "Fox", EmailAddress = "ron@example.com" },
            Interview = new()
            {
                Reviewer = reviewer,
                Status = TutorApplicationInterview.InterviewStatus.AwaitingReviewerConfirmation,
                ProposedDates = [FakeTimeProvider.GetUtcNow().AddDays(4).UtcDateTime]
            }
        };

        await PersonsRepository.AddAsync(reviewer);
        await TutorApplicationsRepository.AddAsync(tutorApplication);
        await TutorApplicationsRepository.SaveChangesAsync();

        var command = new ScheduleInterview.Command(
            tutorApplication.Id,
            tutorApplication.Interview.ProposedDates.First(),
            ScheduleInterview.Party.Applicant); // wrong party

        var result = await Sender.Send(command);

        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(ScheduleInterview.Errors.Handler.InterviewNotInCorrectState.Message, error.Message);
    }
}
