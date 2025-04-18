using Application.UseCases.Queries.TutorApplicationOnboarding.Common;
using Domain.Entities;
using Xcel.TestUtils;

namespace Domain.IntegrationTests.UseCases.Queries.TutorApplicationOnboarding.Common;

public class GetInterviewDetailsByPartyTests : BaseTest
{
    [Fact]
    public async Task Handle_ReturnsInterviewDetails_WhenApplicantRequestsTheirInterview()
    {
        // Arrange
        var applicant = new Person { FirstName = "Alex", LastName = "Stone", EmailAddress = "alex@app.com" };
        var reviewer = new Person { FirstName = "Jordan", LastName = "Ray", EmailAddress = "jordan@mod.com" };

        var application = new TutorApplication
        {
            Applicant = applicant,
            Interview = new TutorApplicationInterview
            {
                Reviewer = reviewer,
                Platform = TutorApplicationInterview.InterviewPlatform.GoogleMeets,
                Status = TutorApplicationInterview.InterviewStatus.AwaitingReviewerConfirmation,
                ProposedDates = [FakeTimeProvider.GetUtcNow().AddDays(1).UtcDateTime],
                Observations = "Looking forward to it"
            }
        };

        await PersonsRepository.AddAsync(applicant);
        await PersonsRepository.AddAsync(reviewer);
        await TutorApplicationsRepository.AddAsync(application);
        await TutorApplicationsRepository.SaveChangesAsync();

        // Act
        var query = new GetInterviewDetailsByParty.Query(application.Id, applicant.Id, GetInterviewDetailsByParty.Party.Applicant);
        var result = await Sender.Send(query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(application.Id, result.Value.ApplicationId);
        Assert.Equal(TutorApplicationInterview.InterviewPlatform.GoogleMeets.ToString(), result.Value.Platform);
        Assert.Equal(TutorApplicationInterview.InterviewStatus.AwaitingReviewerConfirmation.ToString(), result.Value.Status);
        Assert.Equal("Looking forward to it", result.Value.Observations);
        Assert.Equal(reviewer.EmailAddress, result.Value.Reviewer.Email);
        Assert.Equal(applicant.EmailAddress, result.Value.Applicant.Email);
    }

    [Fact]
    public async Task Handle_ReturnsUnauthorized_WhenUserDoesNotMatchParty()
    {
        // Arrange
        var user = new Person { FirstName = "Fake", LastName = "User", EmailAddress = "fake@no.com" };
        var applicant = new Person { FirstName = "True", LastName = "Applicant", EmailAddress = "true@yes.com" };
        var reviewer = new Person { FirstName = "Real", LastName = "Reviewer", EmailAddress = "real@mod.com" };

        var application = new TutorApplication
        {
            Applicant = applicant,
            Interview = new TutorApplicationInterview
            {
                Reviewer = reviewer,
                Platform = TutorApplicationInterview.InterviewPlatform.GoogleMeets,
                Status = TutorApplicationInterview.InterviewStatus.Confirmed
            }
        };

        await PersonsRepository.AddAsync(user);
        await PersonsRepository.AddAsync(applicant);
        await PersonsRepository.AddAsync(reviewer);
        await TutorApplicationsRepository.AddAsync(application);
        await TutorApplicationsRepository.SaveChangesAsync();

        // Act
        var query = new GetInterviewDetailsByParty.Query(application.Id, user.Id, GetInterviewDetailsByParty.Party.Applicant);
        var result = await Sender.Send(query);

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(GetInterviewDetailsByParty.Errors.Unauthorized.Message, error.Message);
    }

    [Fact]
    public async Task Handle_ReturnsNotFound_WhenApplicationOrInterviewMissing()
    {
        // Act
        var query = new GetInterviewDetailsByParty.Query(Guid.NewGuid(), Guid.NewGuid(), GetInterviewDetailsByParty.Party.Applicant);
        var result = await Sender.Send(query);

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(GetInterviewDetailsByParty.Errors.NotFound.Message, error.Message);
    }
}