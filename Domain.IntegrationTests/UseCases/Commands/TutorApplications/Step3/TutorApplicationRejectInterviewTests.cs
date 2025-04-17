using Application.UseCases.Commands.TutorApplications.Step3;
using Domain.Entities;
using Xcel.Services.Email.Templates;
using Xcel.TestUtils;

namespace Domain.IntegrationTests.UseCases.Commands.TutorApplications.Step3;

public class TutorApplicationRejectInterviewTests : BaseTest
{
    [Fact]
    public async Task Handle_Rejection_Succeeds_EmailSent_AccountDeleted()
    {
        // Arrange
        var applicant = new Person { FirstName = "Sara", LastName = "Nash", EmailAddress = "sara.nash@example.com" };
        var reviewer = new Person { FirstName = "Eli", LastName = "Stone", EmailAddress = "eli.stone@example.com" };
        await PersonsRepository.AddRangeAsync([applicant, reviewer]);

        var application = new TutorApplication
        {
            Applicant = applicant,
            Interview = new TutorApplicationInterview
            {
                Reviewer = reviewer,
                ReviewerId = reviewer.Id,
                Status = TutorApplicationInterview.InterviewStatus.Confirmed
            }
        };

        await TutorApplicationsRepository.AddAsync(application);
        await TutorApplicationsRepository.SaveChangesAsync();

        var command = new TutorApplicationRejectInterview.Command(application.Id, "Not a good fit");

        // Act
        var result = await Sender.Send(command);

        // Assert
        Assert.True(result.IsSuccess);

        var updatedTutorApplication = await TutorApplicationsRepository.GetByIdAsync(application.Id);
        Assert.NotNull(updatedTutorApplication);
        Assert.True(updatedTutorApplication.IsRejected);

        var rejectionEmail = InMemoryEmailService.GetSentEmail<TutorInterviewRejectionEmail>();
        Assert.Equal(applicant.EmailAddress, rejectionEmail.Payload.To.First());
        Assert.Equal(applicant.FullName, rejectionEmail.Payload.Data.FullName);
        Assert.Equal("Not a good fit", rejectionEmail.Payload.Data.RejectionReason);

        var deletedApplicant = await PersonsRepository.GetByIdAsync(applicant.Id);
        Assert.Null(deletedApplicant);
    }

    [Fact]
    public async Task Handle_Fails_WhenInterviewIsNotConfirmed()
    {
        // Arrange
        var applicant = new Person { FirstName = "Tim", LastName = "Burton", EmailAddress = "tim.b@example.com" };
        var application = new TutorApplication
        {
            Applicant = applicant,
            Interview = new TutorApplicationInterview
            {
                Reviewer = new Person { FirstName = "Jane", LastName = "Roe", EmailAddress = "jane.roe@example.com" },
                Status = TutorApplicationInterview.InterviewStatus.AwaitingReviewerConfirmation
            }
        };

        await PersonsRepository.AddAsync(applicant);
        await TutorApplicationsRepository.AddAsync(application);
        await TutorApplicationsRepository.SaveChangesAsync();

        var command = new TutorApplicationRejectInterview.Command(application.Id, "Interview not yet confirmed");

        // Act
        var result = await Sender.Send(command);

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal("Interview must be confirmed before rejection.", error.Message);
    }

    [Fact]
    public async Task Handle_Fails_WhenApplicationDoesNotExist()
    {
        // Act
        var command = new TutorApplicationRejectInterview.Command(Guid.NewGuid(), "Not found");
        var result = await Sender.Send(command);

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal("Tutor application not found.", error.Message);
    }
}