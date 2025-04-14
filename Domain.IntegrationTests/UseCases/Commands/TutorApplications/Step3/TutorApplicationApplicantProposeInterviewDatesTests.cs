using Application.UseCases.Commands.TutorApplications.Step3;
using Domain.Entities;
using Xcel.Services.Email.Templates.ReviewerInterviewDatesEmail;
using Xcel.TestUtils;

namespace Domain.IntegrationTests.UseCases.Commands.TutorApplications.Step3;

public class TutorApplicationApplicantProposeInterviewDatesTests : BaseTest
{
    [Fact]
    public async Task Handle_SuccessfullyProposesDatesAndSendsEmail()
    {
        // Arrange
        var applicant = new Person { FirstName = "John", LastName = "Doe", EmailAddress = "john.doe@example.com" };
        var reviewer = new Person { FirstName = "Jane", LastName = "Smith", EmailAddress = "jane.smith@example.com" };
        await PersonsRepository.AddRangeAsync([applicant, reviewer]);

        var tutorApplication = new TutorApplication
        {
            Applicant = applicant,
            Interview = new TutorApplicationInterview
            {
                ReviewerId = reviewer.Id,
                Reviewer = reviewer,
                Status = TutorApplicationInterview.InterviewStatus.AwaitingReviewerProposedDates
            }
        };

        await TutorApplicationsRepository.AddAsync(tutorApplication);
        await TutorApplicationsRepository.SaveChangesAsync();

        var proposedDates = new List<DateTime> { DateTime.UtcNow.AddDays(7), DateTime.UtcNow.AddDays(10) };
        var observations = "These dates work best for me.";
        var command = new TutorApplicationApplicantProposeInterviewDates.Command(tutorApplication.Id, proposedDates, observations);

        // Act
        var result = await Sender.Send(command);

        // Assert
        Assert.True(result.IsSuccess);

        var updatedTutorApplication = await TutorApplicationsRepository.GetByIdAsync(tutorApplication.Id);
        Assert.NotNull(updatedTutorApplication);
        Assert.NotNull(updatedTutorApplication.Interview);
        Assert.Equal(TutorApplicationInterview.InterviewStatus.AwaitingReviewerConfirmation, updatedTutorApplication.Interview.Status);
        Assert.Equal(proposedDates, updatedTutorApplication.Interview.ProposedDates);
        Assert.Equal(observations, updatedTutorApplication.Interview.Observations);

        // Assert email was sent
        var sentEmail = InMemoryEmailSender.GetSentEmail<ReviewerInterviewDatesEmailData>();
        Assert.Equal(reviewer.EmailAddress, sentEmail.Payload.To.First());
        Assert.Equal(applicant.FullName, sentEmail.Payload.Data.ApplicantFullName);
        Assert.Equal(proposedDates, sentEmail.Payload.Data.ProposedDates);
        Assert.Equal(observations, sentEmail.Payload.Data.Observations);
    }
}