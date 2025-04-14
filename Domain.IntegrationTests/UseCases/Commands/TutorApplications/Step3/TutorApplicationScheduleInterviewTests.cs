using Application.UseCases.Commands.TutorApplications.Step3;
using Domain.Entities;
using Xcel.TestUtils;

namespace Domain.IntegrationTests.UseCases.Commands.TutorApplications.Step3;

public class TutorApplicationScheduleInterviewTests : BaseTest
{
    [Fact]
    public async Task Handle_Success_WhenApplicantConfirmsInterview()
    {
        // Arrange
        var applicant = new Person { FirstName = "Liam", LastName = "Taylor", EmailAddress = "liam.taylor@example.com" };
        var reviewer = new Person { FirstName = "Olivia", LastName = "White", EmailAddress = "olivia.white@example.com" };
        await PersonsRepository.AddRangeAsync([applicant, reviewer]);

        var scheduledDate = FakeTimeProvider.GetUtcNow().AddDays(2).UtcDateTime;

        var tutorApplication = new TutorApplication
        {
            Applicant = applicant,
            ApplicantId = applicant.Id,
            Interview = new TutorApplicationInterview
            {
                ReviewerId = reviewer.Id,
                Reviewer = reviewer,
                Status = TutorApplicationInterview.InterviewStatus.AwaitingApplicantConfirmation,
                ProposedDates = [scheduledDate],
                Platform = TutorApplicationInterview.InterviewPlatform.GoogleMeets // Set a default platform
            }
        };

        await TutorApplicationsRepository.AddAsync(tutorApplication);
        await TutorApplicationsRepository.SaveChangesAsync();

        var command = new TutorApplicationScheduleInterview.Command(tutorApplication.Id, scheduledDate, TutorApplicationScheduleInterview.Party.Applicant); // Specify the confirming party

        // Act
        var result = await Sender.Send(command);

        // Assert
        Assert.True(result.IsSuccess);

        var updated = await TutorApplicationsRepository.GetByIdAsync(tutorApplication.Id);
        Assert.NotNull(updated);
        Assert.NotNull(updated.Interview);
        Assert.Equal(TutorApplicationInterview.InterviewStatus.Confirmed, updated.Interview.Status);
        Assert.Equal(scheduledDate, updated.Interview.ScheduledAt);
        Assert.Equal(TutorApplicationInterview.InterviewPlatform.GoogleMeets, updated.Interview.Platform);
        Assert.Equal(applicant.Id, updated.Interview.ConfirmedBy);
    }

    [Fact]
    public async Task Handle_Success_WhenReviewerConfirmsInterview()
    {
        // Arrange
        var applicant = new Person { FirstName = "Emma", LastName = "Johnson", EmailAddress = "emma.johnson@example.com" };
        var reviewer = new Person { FirstName = "Noah", LastName = "Davis", EmailAddress = "noah.davis@example.com" };
        await PersonsRepository.AddRangeAsync([applicant, reviewer]);

        var scheduledDate = FakeTimeProvider.GetUtcNow().AddDays(3).UtcDateTime;

        var tutorApplication = new TutorApplication
        {
            Applicant = applicant,
            ApplicantId = applicant.Id, 
            Interview = new TutorApplicationInterview
            {
                ReviewerId = reviewer.Id,
                Reviewer = reviewer,
                Status = TutorApplicationInterview.InterviewStatus.AwaitingReviewerConfirmation,
                ProposedDates = [scheduledDate],
                Platform = TutorApplicationInterview.InterviewPlatform.GoogleMeets
            }
        };

        await TutorApplicationsRepository.AddAsync(tutorApplication);
        await TutorApplicationsRepository.SaveChangesAsync();

        var command = new TutorApplicationScheduleInterview.Command(tutorApplication.Id, scheduledDate, TutorApplicationScheduleInterview.Party.Reviewer); // Specify the confirming party

        // Act
        var result = await Sender.Send(command);

        // Assert
        Assert.True(result.IsSuccess);

        var updated = await TutorApplicationsRepository.GetByIdAsync(tutorApplication.Id);
        Assert.NotNull(updated);
        Assert.NotNull(updated.Interview);
        Assert.Equal(TutorApplicationInterview.InterviewStatus.Confirmed, updated.Interview.Status);
        Assert.Equal(scheduledDate, updated.Interview.ScheduledAt);
        Assert.Equal(TutorApplicationInterview.InterviewPlatform.GoogleMeets, updated.Interview.Platform);
        Assert.Equal(reviewer.Id, updated.Interview.ConfirmedBy);
    }
}