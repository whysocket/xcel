using Application.UseCases.Commands.TutorApplications.Step3.AfterInterview;
using Application.UseCases.Commands.TutorApplications.Step3.AfterInterview.Reviewer;
using Domain.Entities;
using Xcel.Services.Email.Templates;
using Xcel.TestUtils;

namespace Domain.IntegrationTests.UseCases.Commands.TutorApplications.Step3.AfterInterview.Reviewer;

public class RejectInterviewTests : BaseTest
{
    [Fact]
    public async Task Handle_RejectsInterviewAndDeletesAccount_Successfully()
    {
        // Arrange
        var reviewer = new Person { FirstName = "Ismael", LastName = "Sun", EmailAddress = "reviewer@example.com" };
        var applicant = new Person { FirstName = "Nora", LastName = "West", EmailAddress = "nora@example.com" };
        var tutorApplication = new TutorApplication
        {
            Applicant = applicant,
            Interview = new()
            {
                Reviewer = reviewer,
                Status = TutorApplicationInterview.InterviewStatus.Confirmed
            }
        };

        await PersonsRepository.AddRangeAsync([reviewer, applicant]);
        await TutorApplicationsRepository.AddAsync(tutorApplication);
        await TutorApplicationsRepository.SaveChangesAsync();

        var command = new RejectInterview.Command(tutorApplication.Id, "Not a good fit");

        // Act
        var result = await Sender.Send(command);

        // Assert
        Assert.True(result.IsSuccess);

        var updatedTutorApplication = await TutorApplicationsRepository.GetByIdAsync(tutorApplication.Id);
        Assert.NotNull(updatedTutorApplication);
        Assert.True(updatedTutorApplication.IsRejected);

        var sentEmail = InMemoryEmailService.GetSentEmail<TutorInterviewRejectionEmail>();
        Assert.Equal(applicant.EmailAddress, sentEmail.Payload.To.First());
        Assert.Equal(applicant.FullName, sentEmail.Payload.Data.FullName);
        Assert.Equal("Not a good fit", sentEmail.Payload.Data.RejectionReason);

        var deletedApplicant = await PersonsRepository.GetByIdAsync(applicant.Id);
        Assert.Null(deletedApplicant);
    }

    [Fact]
    public async Task Handle_ReturnsFailure_WhenInterviewIsNotConfirmed()
    {
        // Arrange
        var applicant = new Person { FirstName = "Leo", LastName = "Moon", EmailAddress = "leo@example.com" };
        var tutorApplication = new TutorApplication
        {
            Applicant = applicant,
            Interview = new()
            {
                Reviewer = new Person { FirstName = "Mod", LastName = "Bot", EmailAddress = "mod@example.com" },
                Status = TutorApplicationInterview.InterviewStatus.AwaitingApplicantConfirmation
            }
        };

        await PersonsRepository.AddAsync(applicant);
        await TutorApplicationsRepository.AddAsync(tutorApplication);
        await TutorApplicationsRepository.SaveChangesAsync();

        var command = new RejectInterview.Command(tutorApplication.Id);

        // Act
        var result = await Sender.Send(command);

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(RejectInterview.Errors.Handler.InvalidInterviewState.Message, error.Message);
    }

    [Fact]
    public async Task Handle_ReturnsFailure_WhenApplicationNotFound()
    {
        // Arrange
        var command = new RejectInterview.Command(Guid.NewGuid(), "Reason");

        // Act
        var result = await Sender.Send(command);

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(RejectInterview.Errors.Handler.TutorApplicationNotFound.Message, error.Message);
    }
}
