using Application.UseCases.Commands.TutorApplications.Step3.BookInterview;
using Application.UseCases.Commands.TutorApplications.Step3.BookInterview.Reviewer;
using Domain.Entities;
using Xcel.Services.Email.Templates;
using Xcel.TestUtils;

namespace Domain.IntegrationTests.UseCases.Commands.TutorApplications.Step3.BookInterview.Reviewer;

public class ReviewerProposeInterviewDatesTests : BaseTest
{
    [Fact]
    public async Task Handle_ReviewerProposesDates_Successfully()
    {
        // Arrange
        var reviewer = new Person { FirstName = "Jane", LastName = "Smith", EmailAddress = "jane@example.com" };
        var applicant = new Person { FirstName = "Leo", LastName = "King", EmailAddress = "leo@example.com" };
        var tutorApplication = new TutorApplication
        {
            Applicant = applicant,
            Interview = new()
            {
                Reviewer = reviewer,
                Status = TutorApplicationInterview.InterviewStatus.AwaitingReviewerConfirmation
            }
        };

        await PersonsRepository.AddRangeAsync([reviewer, applicant]);
        await TutorApplicationsRepository.AddAsync(tutorApplication);
        await TutorApplicationsRepository.SaveChangesAsync();

        var proposedDates = new List<DateTime>
        {
            FakeTimeProvider.GetUtcNow().AddDays(1).UtcDateTime,
            FakeTimeProvider.GetUtcNow().AddDays(2).UtcDateTime
        };
        var observations = "Flexible on times after 3pm.";
        var command = new ReviewerProposeInterviewDates.Command(
            tutorApplication.Id,
            proposedDates,
            observations
        );

        // Act
        var result = await Sender.Send(command);

        // Assert
        Assert.True(result.IsSuccess);

        var updatedTutorApplication = await TutorApplicationsRepository.GetByIdAsync(tutorApplication.Id);
        Assert.NotNull(updatedTutorApplication);
        Assert.Equal(TutorApplicationInterview.InterviewStatus.AwaitingApplicantConfirmation,
            updatedTutorApplication.Interview!.Status);
        Assert.Equal(proposedDates, updatedTutorApplication.Interview.ProposedDates);
        Assert.Equal(observations, updatedTutorApplication.Interview.Observations);

        var sentEmail = InMemoryEmailService.GetSentEmail<TutorApplicantProposedDatesEmail>();
        Assert.Equal(applicant.EmailAddress, sentEmail.Payload.To.First());
        Assert.Equal(applicant.FullName, sentEmail.Payload.Data.ApplicantFullName);
        Assert.Equal(proposedDates, sentEmail.Payload.Data.ProposedDatesUtc);
        Assert.Equal(observations, sentEmail.Payload.Data.Observations);
    }

    [Fact]
    public async Task Handle_ReturnsFailure_WhenInterviewNotInCorrectState()
    {
        // Arrange
        var tutorApplication = new TutorApplication
        {
            Applicant = new Person { FirstName = "Mina", LastName = "Wave", EmailAddress = "mina@example.com" },
            Interview = new()
            {
                Reviewer = new Person { FirstName = "Joe", LastName = "Mod", EmailAddress = "mod@example.com" },
                Status = TutorApplicationInterview.InterviewStatus.Confirmed
            }
        };

        await TutorApplicationsRepository.AddAsync(tutorApplication);
        await TutorApplicationsRepository.SaveChangesAsync();

        var command = new ReviewerProposeInterviewDates.Command(
            tutorApplication.Id,
            [FakeTimeProvider.GetUtcNow().AddDays(3).UtcDateTime],
            "Try again"
        );

        // Act
        var result = await Sender.Send(command);

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(ReviewerProposeInterviewDates.Errors.Handler.InterviewNotInCorrectState.Message, error.Message);
    }

    [Fact]
    public async Task Handle_ReturnsFailure_WhenTutorApplicationNotFound()
    {
        // Arrange
        var command = new ReviewerProposeInterviewDates.Command(
            Guid.NewGuid(),
            [FakeTimeProvider.GetUtcNow().AddDays(2).UtcDateTime],
            null
        );

        // Act
        var result = await Sender.Send(command);

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(ReviewerProposeInterviewDates.Errors.Handler.TutorApplicationNotFound.Message, error.Message);
    }
}
