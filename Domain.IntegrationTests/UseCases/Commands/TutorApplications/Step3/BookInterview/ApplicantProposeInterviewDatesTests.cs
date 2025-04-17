using Application.UseCases.Commands.TutorApplications.Step3.BookInterview;
using Domain.Entities;
using Domain.Exceptions;
using Xcel.Services.Email.Templates;
using Xcel.TestUtils;

namespace Domain.IntegrationTests.UseCases.Commands.TutorApplications.Step3.BookInterview;

public class ApplicantProposeInterviewDatesTests : BaseTest
{
    [Fact]
    public async Task Handle_ProposesInterviewDates_Successfully()
    {
        // Arrange
        var applicant = new Person { FirstName = "Aria", LastName = "Moon", EmailAddress = "aria@example.com" };
        var reviewer = new Person { FirstName = "Jay", LastName = "Stone", EmailAddress = "jay@example.com" };
        var interviewDates = new List<DateTime>
        {
            FakeTimeProvider.GetUtcNow().AddDays(3).UtcDateTime,
            FakeTimeProvider.GetUtcNow().AddDays(5).UtcDateTime
        };

        var application = new TutorApplication
        {
            Applicant = applicant,
            Interview = new TutorApplicationInterview
            {
                Reviewer = reviewer,
                Status = TutorApplicationInterview.InterviewStatus.AwaitingReviewerProposedDates
            }
        };

        await PersonsRepository.AddRangeAsync([applicant, reviewer]);
        await TutorApplicationsRepository.AddAsync(application);
        await TutorApplicationsRepository.SaveChangesAsync();

        var observations = "Evenings preferred";
        var command = new ApplicantProposeInterviewDates.Command(application.Id, interviewDates, observations);

        // Act
        var result = await Sender.Send(command);

        // Assert
        Assert.True(result.IsSuccess);

        var updatedApplication = await TutorApplicationsRepository.GetByIdAsync(application.Id);
        Assert.NotNull(updatedApplication);
        var updatedInterview = updatedApplication.Interview;
        Assert.NotNull(updatedInterview);
        Assert.Equal(TutorApplicationInterview.InterviewStatus.AwaitingReviewerConfirmation, updatedInterview.Status);
        Assert.Equal(interviewDates, updatedInterview.ProposedDates);
        Assert.Equal(observations, updatedInterview.Observations);

        var sentEmail = InMemoryEmailService.GetSentEmail<ReviewerInterviewDatesEmail>();
        Assert.Equal(reviewer.EmailAddress, sentEmail.Payload.To.First());
        Assert.Equal(applicant.FullName, sentEmail.Payload.Data.ApplicantFullName);
        Assert.Equal(interviewDates, sentEmail.Payload.Data.ProposedDatesUtc);
        Assert.Equal(observations, sentEmail.Payload.Data.Observations);
    }

    [Fact]
    public async Task Handle_Fails_WhenInterviewInInvalidState()
    {
        // Arrange
        var applicant = new Person { FirstName = "Liam", LastName = "Brooks", EmailAddress = "liam@example.com" };
        var reviewer = new Person { FirstName = "Ivy", LastName = "Hill", EmailAddress = "ivy@example.com" };

        var application = new TutorApplication
        {
            Applicant = applicant,
            Interview = new TutorApplicationInterview
            {
                Reviewer = reviewer,
                Status = TutorApplicationInterview.InterviewStatus.Confirmed // invalid for proposal
            }
        };

        await PersonsRepository.AddRangeAsync([applicant, reviewer]);
        await TutorApplicationsRepository.AddAsync(application);
        await TutorApplicationsRepository.SaveChangesAsync();

        var interviewDates = new List<DateTime> { FakeTimeProvider.GetUtcNow().AddDays(3).UtcDateTime };
        var command = new ApplicantProposeInterviewDates.Command(application.Id, interviewDates, null);

        // Act
        var result = await Sender.Send(command);

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(ApplicantProposeInterviewDates.Errors.Handler.InterviewNotInCorrectState.Message, error.Message);
    }

    [Fact]
    public async Task Handle_ThrowsValidation_WhenDatesAreEmpty()
    {
        // Arrange
        var tutorApplicationId = Guid.NewGuid();
        var command = new ApplicantProposeInterviewDates.Command(tutorApplicationId, [], null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<DomainValidationException>(() => Sender.Send(command));
        var error = Assert.Single(exception.ToResult().Errors);
        Assert.Equal(ApplicantProposeInterviewDates.Errors.Commnad.AtLeastOneProposeDateIsRequired, error.Message);
    }

    [Fact]
    public async Task Handle_ThrowsValidation_WhenDatesAreInPast()
    {
        // Arrange
        var tutorApplicationId = Guid.NewGuid();
        var command = new ApplicantProposeInterviewDates.Command(
            tutorApplicationId,
            [FakeTimeProvider.GetUtcNow().AddDays(-1).UtcDateTime],
            null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<DomainValidationException>(() => Sender.Send(command));
        var error = Assert.Single(exception.ToResult().Errors);
        Assert.Equal(ApplicantProposeInterviewDates.Errors.Commnad.AllProposedDatesInFuture, error.Message);
    }

    [Fact]
    public async Task Handle_ThrowsValidation_WhenMoreThanThreeDates()
    {
        // Arrange
        var tutorApplicationId = Guid.NewGuid();
        var command = new ApplicantProposeInterviewDates.Command(
            tutorApplicationId,
            [
                FakeTimeProvider.GetUtcNow().AddDays(1).UtcDateTime,
                FakeTimeProvider.GetUtcNow().AddDays(2).UtcDateTime,
                FakeTimeProvider.GetUtcNow().AddDays(3).UtcDateTime,
                FakeTimeProvider.GetUtcNow().AddDays(4).UtcDateTime,
            ],
            null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<DomainValidationException>(() => Sender.Send(command));
        var error = Assert.Single(exception.ToResult().Errors);
        Assert.Equal(ApplicantProposeInterviewDates.Errors.Commnad.ProposeUpToThreeDates, error.Message);
    }
}