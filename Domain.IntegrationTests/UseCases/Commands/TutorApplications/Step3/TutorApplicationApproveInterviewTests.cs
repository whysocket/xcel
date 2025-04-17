using Application.UseCases.Commands.TutorApplications.Step3;
using Domain.Entities;
using Xcel.Services.Email.Templates;
using Xcel.TestUtils;

namespace Domain.IntegrationTests.UseCases.Commands.TutorApplications.Step3;

public class TutorApplicationApproveInterviewTests : BaseTest
{
    [Fact]
    public async Task Handle_ApprovesInterview_AdvancesStep_AndSendsEmail()
    {
        // Arrange
        var applicant = new Person { FirstName = "Alex", LastName = "Taylor", EmailAddress = "alex.taylor@example.com" };
        var reviewer = new Person { FirstName = "Maya", LastName = "Lee", EmailAddress = "maya.lee@example.com" };
        await PersonsRepository.AddRangeAsync([applicant, reviewer]);

        var tutorApplication = new TutorApplication
        {
            Applicant = applicant,
            Interview = new TutorApplicationInterview
            {
                ReviewerId = reviewer.Id,
                Reviewer = reviewer,
                Status = TutorApplicationInterview.InterviewStatus.Confirmed
            }
        };

        await TutorApplicationsRepository.AddAsync(tutorApplication);
        await TutorApplicationsRepository.SaveChangesAsync();

        var command = new TutorApplicationApproveInterview.Command(tutorApplication.Id);

        // Act
        var result = await Sender.Send(command);

        // Assert
        Assert.True(result.IsSuccess);

        var updated = await TutorApplicationsRepository.GetByIdAsync(tutorApplication.Id);
        Assert.Equal(TutorApplication.OnboardingStep.DocumentsRequested, updated.CurrentStep);

        var sentEmail = InMemoryEmailService.GetSentEmail<TutorRequestDocumentsEmail>();
        Assert.Equal(applicant.EmailAddress, sentEmail.Payload.To.First());
        Assert.Equal(applicant.FullName, sentEmail.Payload.Data.FullName);
    }

    [Fact]
    public async Task Handle_Fails_WhenInterviewNotConfirmed()
    {
        // Arrange
        var applicant = new Person { FirstName = "Sam", LastName = "Green", EmailAddress = "sam.green@example.com" };
        await PersonsRepository.AddAsync(applicant);

        var tutorApplication = new TutorApplication
        {
            Applicant = applicant,
            Interview = new TutorApplicationInterview
            {
                Reviewer = new Person { FirstName = "Reviewer", LastName = "One", EmailAddress = "rev@example.com" },
                Status = TutorApplicationInterview.InterviewStatus.AwaitingApplicantConfirmation
            }
        };

        await TutorApplicationsRepository.AddAsync(tutorApplication);
        await TutorApplicationsRepository.SaveChangesAsync();

        var command = new TutorApplicationApproveInterview.Command(tutorApplication.Id);

        // Act
        var result = await Sender.Send(command);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, e => e.Message.Contains("Interview must be confirmed"));
    }
}