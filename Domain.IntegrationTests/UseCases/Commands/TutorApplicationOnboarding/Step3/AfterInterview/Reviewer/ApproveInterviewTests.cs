using Application.UseCases.Commands.TutorApplicationOnboarding.Step3.AfterInterview.Reviewer;
using Domain.Entities;
using Xcel.Services.Email.Templates;
using Xcel.TestUtils;

namespace Domain.IntegrationTests.UseCases.Commands.TutorApplicationOnboarding.Step3.AfterInterview.Reviewer;

public class ApproveInterviewTests : BaseTest
{
    [Fact]
    public async Task Handle_ApprovesInterviewAndSendsEmailSuccessfully()
    {
        // Arrange
        var reviewer = new Person { FirstName = "Ismael", LastName = "Sun", EmailAddress = "reviewerisma@example.com" };
        var applicant = new Person { FirstName = "Tina", LastName = "West", EmailAddress = "tina@example.com" };
        var tutorApplication = new TutorApplication
        {
            Applicant = applicant,
            CurrentStep = TutorApplication.OnboardingStep.InterviewScheduled,
            Interview = new()
            {
                Reviewer = reviewer,
                Status = TutorApplicationInterview.InterviewStatus.Confirmed
            }
        };

        await PersonsRepository.AddAsync(applicant);
        await TutorApplicationsRepository.AddAsync(tutorApplication);
        await TutorApplicationsRepository.SaveChangesAsync();

        var command = new ApproveInterview.Command(tutorApplication.Id);

        // Act
        var result = await Sender.Send(command);

        // Assert
        Assert.True(result.IsSuccess);

        var updatedTutorApplication = await TutorApplicationsRepository.GetByIdAsync(tutorApplication.Id);
        Assert.NotNull(updatedTutorApplication);
        Assert.Equal(TutorApplication.OnboardingStep.DocumentsRequested, updatedTutorApplication.CurrentStep);

        var sentEmail = InMemoryEmailService.GetSentEmail<TutorRequestDocumentsEmail>();
        Assert.Equal(applicant.EmailAddress, sentEmail.Payload.To.First());
        Assert.Equal(applicant.FullName, sentEmail.Payload.Data.FullName);
    }

    [Fact]
    public async Task Handle_ReturnsFailure_WhenInterviewIsNotConfirmed()
    {
        // Arrange
        var reviewer = new Person { FirstName = "Ismael", LastName = "Sun", EmailAddress = "reviewerisma@example.com" };
        var tutorApplication = new TutorApplication
        {
            Applicant = new Person { FirstName = "Liam", LastName = "Ray", EmailAddress = "liam@example.com" },
            Interview = new()
            {
                Reviewer = reviewer,
                Status = TutorApplicationInterview.InterviewStatus.AwaitingReviewerConfirmation
            }
        };

        await TutorApplicationsRepository.AddAsync(tutorApplication);
        await TutorApplicationsRepository.SaveChangesAsync();

        var command = new ApproveInterview.Command(tutorApplication.Id);

        // Act
        var result = await Sender.Send(command);

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(ApproveInterview.Errors.Handler.InvalidInterviewState.Message, error.Message);
    }
} 
