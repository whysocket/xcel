using Application.UseCases.Queries.TutorApplicationOnboarding.Applicant;
using Domain.Constants;
using Domain.Entities;
using Domain.Results;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xcel.Services.Auth.Public;
using Xcel.TestUtils;

namespace Domain.IntegrationTests.UseCases.Queries.TutorApplicationOnboarding.Applicant;

public class GetMyTutorApplicationQueryTests : BaseTest
{
    private readonly IAuthServiceSdk _authService = Substitute.For<IAuthServiceSdk>();
    private readonly ILogger<GetMyTutorApplicationQuery> _logger = Substitute.For<ILogger<GetMyTutorApplicationQuery>>();

    [Fact]
    public async Task ExecuteAsync_ReturnsApplication_WhenUserHasNoDisallowedRoles()
    {
        // Arrange
        var user = new Person { FirstName = "Valid", LastName = "User", EmailAddress = "valid@app.com" };
        var reviewer = new Person { FirstName = "Mod", LastName = "User", EmailAddress = "mod@xceltutors.com" };

        var application = new TutorApplication
        {
            Applicant = user,
            Documents = [],
            Interview = new TutorApplicationInterview
            {
                Reviewer = reviewer,
                Platform = TutorApplicationInterview.InterviewPlatform.GoogleMeets,
                Status = TutorApplicationInterview.InterviewStatus.AwaitingReviewerConfirmation,
                ProposedDates = [FakeTimeProvider.GetUtcNow().AddDays(1).UtcDateTime],
                Observations = "All good"
            }
        };

        await PersonsRepository.AddRangeAsync([user, reviewer]);
        await TutorApplicationsRepository.AddAsync(application);
        await TutorApplicationsRepository.SaveChangesAsync();

        _authService.GetRolesByPersonIdAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns(Result.Ok(new List<Role>()));

        var query = new GetMyTutorApplicationQuery(TutorApplicationsRepository, _authService, _logger);

        // Act
        var result = await query.ExecuteAsync(user.Id);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(application.Id, result.Value.Id);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsConflict_WhenUserHasDisallowedRole()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _authService.GetRolesByPersonIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Result.Ok(new List<Role> { new() { Id = Guid.NewGuid(), Name = UserRoles.Reviewer } }));

        var query = new GetMyTutorApplicationQuery(TutorApplicationsRepository, _authService, _logger);

        // Act
        var result = await query.ExecuteAsync(userId);

        // Assert
        Assert.True(result.IsFailure);
        var resultError = Assert.Single(result.Errors);
        Assert.Equal(GetMyTutorApplicationQueryErrors.AlreadyHasRole(userId), resultError);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsNotFound_WhenNoApplicationExists()
    {
        // Arrange
        var user = new Person { FirstName = "Ghost", LastName = "User", EmailAddress = "ghost@app.com" };
        await PersonsRepository.AddAsync(user);
        await PersonsRepository.SaveChangesAsync();

        _authService.GetRolesByPersonIdAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns(Result.Ok(new List<Role>()));

        var query = new GetMyTutorApplicationQuery(TutorApplicationsRepository, _authService, _logger);

        // Act
        var result = await query.ExecuteAsync(user.Id);

        // Assert
        Assert.True(result.IsFailure);
        var resultError = Assert.Single(result.Errors);
        Assert.Equal(GetMyTutorApplicationQueryErrors.NotFound(user.Id), resultError);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFailure_WhenAuthServiceFails()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var mockError = new Error(ErrorType.Unexpected, "Auth service unavailable");

        _authService.GetRolesByPersonIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Result.Fail<List<Role>>(mockError));

        var query = new GetMyTutorApplicationQuery(TutorApplicationsRepository, _authService, _logger);

        // Act
        var result = await query.ExecuteAsync(userId);

        // Assert
        Assert.True(result.IsFailure);
        var resultError = Assert.Single(result.Errors);
        Assert.Equal(mockError, resultError);
    }
}
