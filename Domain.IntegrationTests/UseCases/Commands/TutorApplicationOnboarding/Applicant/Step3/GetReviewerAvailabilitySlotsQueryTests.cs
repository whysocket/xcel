using Application.UseCases.Queries.Availability;
using Application.UseCases.Queries.TutorApplicationOnboarding.Applicant.Step3;
using Domain.Entities;
using Domain.Results;
using NSubstitute;
using Xcel.TestUtils;

namespace Domain.IntegrationTests.UseCases.Commands.TutorApplicationOnboarding.Applicant.Step3;

public class GetReviewerAvailabilitySlotsQueryTests : BaseTest
{
    private IGetReviewerAvailabilitySlotsQuery _query = null!;
    private IGetAvailabilitySlotsQuery _availabilityQueryMock = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        _availabilityQueryMock = Substitute.For<IGetAvailabilitySlotsQuery>();
        _query = new GetReviewerAvailabilitySlotsQuery(
            TutorApplicationsRepository,
            _availabilityQueryMock,
            FakeTimeProvider,
            CreateLogger<GetReviewerAvailabilitySlotsQuery>()
        );
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnSlots_WhenValidApplicationAndReviewer()
    {
        // Scenario: Applicant fetches availability for their assigned reviewer successfully.
        // Arrange
        var now = FakeTimeProvider.GetUtcNow().UtcDateTime;
        var today = DateOnly.FromDateTime(now);

        var applicant = new Person
        {
            FirstName = "Alice",
            LastName = "Smith",
            EmailAddress = "alice@example.com",
        };
        var reviewer = new Person
        {
            FirstName = "John",
            LastName = "Reviewer",
            EmailAddress = "john@example.com",
        };
        var application = new TutorApplication
        {
            ApplicantId = applicant.Id, // Ensure ApplicantId is set
            Applicant = applicant,
            Interview = new TutorApplicationInterview
            {
                ReviewerId = reviewer.Id,
                Reviewer = reviewer,
            }, // Ensure ReviewerId is set
        };

        // Add entities to the database
        await PersonsRepository.AddRangeAsync([applicant, reviewer]);
        await TutorApplicationsRepository.AddAsync(application);
        await TutorApplicationsRepository.SaveChangesAsync();

        // Define the expected output from the mocked availability query using NSubstitute's Returns
        var expectedSlotsFromInnerQuery = new List<AvailableSlot>
        {
            new(now.AddHours(13), now.AddHours(13.5)),
        };
        _availabilityQueryMock
            .ExecuteAsync(Arg.Any<AvailabilitySlotsQueryInput>(), Arg.Any<CancellationToken>()) // Use Arg.Any for parameters
            .Returns(Result.Ok(expectedSlotsFromInnerQuery));

        // Act
        var result = await _query.ExecuteAsync(application.ApplicantId, today);

        // Assert
        Assert.True(result.IsSuccess);
        // Verify the output format is correct (TimeSlot)
        var slot = Assert.Single(result.Value);
        Assert.Equal(expectedSlotsFromInnerQuery[0].StartUtc, slot.StartUtc);
        Assert.Equal(expectedSlotsFromInnerQuery[0].EndUtc, slot.EndUtc);

        // Verify the mocked availability query was called exactly once with correct parameters using NSubstitute's Received
        await _availabilityQueryMock
            .Received(1)
            .ExecuteAsync(
                Arg.Is<AvailabilitySlotsQueryInput>(input => // Use Arg.Is to check properties of the input object
                    input.OwnerId == reviewer.Id
                    && input.OwnerType == AvailabilityOwnerType.Reviewer
                    && input.SlotDuration == TimeSpan.FromMinutes(30)
                    && input.FromUtc >= today.ToDateTime(TimeOnly.MinValue)
                    && // Check FromUtc is at least the start of the day
                    input.ToUtc == today.ToDateTime(TimeOnly.MaxValue) // Check ToUtc is the end of the day
                ),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnSlotsStartingFromNow_WhenRequestedDateIsInPast()
    {
        // Scenario: Applicant requests availability for a date in the past.
        // The query should fetch slots starting from the current time, not the beginning of the past day.
        // Arrange
        var now = FakeTimeProvider.GetUtcNow().UtcDateTime;
        var pastDate = DateOnly.FromDateTime(now).AddDays(-1); // Yesterday

        var applicant = new Person
        {
            FirstName = "Past",
            LastName = "Date",
            EmailAddress = "past@example.com",
        };
        var reviewer = new Person
        {
            FirstName = "Future",
            LastName = "Only",
            EmailAddress = "future@example.com",
        };
        var application = new TutorApplication
        {
            ApplicantId = applicant.Id,
            Applicant = applicant,
            Interview = new TutorApplicationInterview
            {
                ReviewerId = reviewer.Id,
                Reviewer = reviewer,
            },
        };

        await PersonsRepository.AddRangeAsync([applicant, reviewer]);
        await TutorApplicationsRepository.AddAsync(application);
        await TutorApplicationsRepository.SaveChangesAsync();

        var expectedSlotsFromInnerQuery = new List<AvailableSlot>
        {
            new(now.AddHours(1), now.AddHours(1.5)),
        }; // Slot is in the future relative to 'now'
        _availabilityQueryMock
            .ExecuteAsync(Arg.Any<AvailabilitySlotsQueryInput>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok(expectedSlotsFromInnerQuery));

        // Act
        var result = await _query.ExecuteAsync(application.ApplicantId, pastDate);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value); // Verify slots are returned

        // Verify the mocked availability query was called with the correct parameters, specifically FromUtc >= now
        await _availabilityQueryMock
            .Received(1)
            .ExecuteAsync(
                Arg.Is<AvailabilitySlotsQueryInput>(input =>
                    input.OwnerId == reviewer.Id
                    && input.OwnerType == AvailabilityOwnerType.Reviewer
                    && input.FromUtc >= now
                    && // This is the key check: FromUtc starts from 'now', not beginning of pastDate
                    input.ToUtc == pastDate.ToDateTime(TimeOnly.MaxValue) // ToUtc is still the end of the requested day
                ),
                Arg.Any<CancellationToken>()
            );
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenUserIsNotApplicant()
    {
        // Scenario: A user tries to fetch availability for an application they do not own.
        // Arrange
        var now = FakeTimeProvider.GetUtcNow().UtcDateTime;
        var today = DateOnly.FromDateTime(now);

        var nonApplicantUser = new Person
        {
            FirstName = "Fake",
            LastName = "User",
            EmailAddress = "fake@xcel.com",
        };
        var realApplicant = new Person
        {
            FirstName = "Real",
            LastName = "User",
            EmailAddress = "real@xcel.com",
        };
        var reviewer = new Person
        {
            FirstName = "Dan",
            LastName = "Reviewer",
            EmailAddress = "dan@xcel.com",
        };

        var application = new TutorApplication
        {
            ApplicantId = realApplicant.Id, // Application belongs to RealUser
            Applicant = realApplicant,
            Interview = new() { ReviewerId = reviewer.Id, Reviewer = reviewer },
            Id = Guid.NewGuid(), // Ensure application has an ID
        };

        // Add entities to the database
        await PersonsRepository.AddRangeAsync([nonApplicantUser, realApplicant, reviewer]);
        await TutorApplicationsRepository.AddAsync(application);
        await TutorApplicationsRepository.SaveChangesAsync();

        // Mock the availability query to return slots (shouldn't be called in this scenario)
        var expectedSlotsFromInnerQuery = new List<AvailableSlot>
        {
            new(now.AddHours(13), now.AddHours(13.5)),
        };
        _availabilityQueryMock
            .ExecuteAsync(Arg.Any<AvailabilitySlotsQueryInput>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok(expectedSlotsFromInnerQuery));

        // Act
        // Call the query with the *non-applicant user's* ID
        var result = await _query.ExecuteAsync(
            nonApplicantUser.Id, // User ID does not match ApplicantId of the application in the DB
            today
        );

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        // FIX: Based on code trace, when GetByUserIdAsync(nonApplicantUser.Id) returns null,
        // the error is TutorApplicationNotFound, NOT Unauthorized.
        Assert.Equal(GetReviewerAvailabilitySlotsQueryErrors.TutorApplicationNotFound, error);

        // Verify the availability query was NOT called using NSubstitute's DidNotReceive
        await _availabilityQueryMock
            .DidNotReceiveWithAnyArgs()
            .ExecuteAsync(Arg.Any<AvailabilitySlotsQueryInput>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenApplicantApplicationNotFound()
    {
        // Scenario: The provided applicant ID does not correspond to any tutor application in the database.
        // Arrange
        var now = FakeTimeProvider.GetUtcNow().UtcDateTime;
        var today = DateOnly.FromDateTime(now);
        var nonExistentApplicantId = Guid.NewGuid();

        // No application is added for nonExistentApplicantId

        // Mock the availability query to return slots (shouldn't be called)
        var expectedSlotsFromInnerQuery = new List<AvailableSlot>
        {
            new(now.AddHours(13), now.AddHours(13.5)),
        };
        _availabilityQueryMock
            .ExecuteAsync(Arg.Any<AvailabilitySlotsQueryInput>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok(expectedSlotsFromInnerQuery));

        // Act
        var result = await _query.ExecuteAsync(
            nonExistentApplicantId, // User ID for which no application exists
            today
        );

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(GetReviewerAvailabilitySlotsQueryErrors.TutorApplicationNotFound, error);

        // Verify the availability query was NOT called
        await _availabilityQueryMock
            .DidNotReceiveWithAnyArgs()
            .ExecuteAsync(Arg.Any<AvailabilitySlotsQueryInput>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenNoReviewerAssignedToApplication()
    {
        // Scenario: The applicant's application does not have a reviewer assigned to the interview.
        // Arrange
        var now = FakeTimeProvider.GetUtcNow().UtcDateTime;
        var today = DateOnly.FromDateTime(now);

        var applicant = new Person
        {
            FirstName = "Jake",
            LastName = "NoReviewer",
            EmailAddress = "jake@xcel.com",
        };
        // Application with an interview that has no reviewer
        var application = new TutorApplication
        {
            ApplicantId = applicant.Id,
            Applicant = applicant,
            // FIX: Set Interview to null instead of creating an interview with Guid.Empty ReviewerId
            Interview = null,
        };
        // Original code causing FK violation:
        // Interview = new TutorApplicationInterview() { Reviewer = null!, ReviewerId = Guid.Empty }

        await PersonsRepository.AddAsync(applicant);
        await TutorApplicationsRepository.AddAsync(application);
        await TutorApplicationsRepository.SaveChangesAsync();

        // Mock the availability query to return slots (shouldn't be called)
        var expectedSlotsFromInnerQuery = new List<AvailableSlot>
        {
            new(now.AddHours(13), now.AddHours(13.5)),
        };
        _availabilityQueryMock
            .ExecuteAsync(Arg.Any<AvailabilitySlotsQueryInput>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok(expectedSlotsFromInnerQuery));

        // Act
        var result = await _query.ExecuteAsync(applicant.Id, today);

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(
            GetReviewerAvailabilitySlotsQueryErrors.ReviewerNotAssigned(application.Id),
            error
        );

        // Verify the availability query was NOT called
        await _availabilityQueryMock
            .DidNotReceiveWithAnyArgs()
            .ExecuteAsync(Arg.Any<AvailabilitySlotsQueryInput>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenInnerAvailabilityQueryFails()
    {
        // Scenario: The inner query responsible for calculating availability slots returns a failure.
        // The outer query should propagate this failure.
        // Arrange
        var now = FakeTimeProvider.GetUtcNow().UtcDateTime;
        var today = DateOnly.FromDateTime(now);

        var applicant = new Person
        {
            FirstName = "Fail",
            LastName = "Query",
            EmailAddress = "fail@xcel.com",
        };
        var reviewer = new Person
        {
            FirstName = "Down",
            LastName = "Service",
            EmailAddress = "down@xcel.com",
        };
        var application = new TutorApplication
        {
            ApplicantId = applicant.Id,
            Applicant = applicant,
            Interview = new() { ReviewerId = reviewer.Id, Reviewer = reviewer },
        };

        await PersonsRepository.AddRangeAsync([applicant, reviewer]);
        await TutorApplicationsRepository.AddAsync(application);
        await TutorApplicationsRepository.SaveChangesAsync();

        // Mock the availability query to RETURN a failure result using NSubstitute's Returns
        var innerQueryError = new Error(ErrorType.Unexpected, "System failure calculating slots.");
        _availabilityQueryMock
            .ExecuteAsync(Arg.Any<AvailabilitySlotsQueryInput>(), Arg.Any<CancellationToken>())
            .Returns(Result.Fail<List<AvailableSlot>>(innerQueryError));

        // Act
        var result = await _query.ExecuteAsync(applicant.Id, today);

        // Assert
        Assert.True(result.IsFailure);
        // Verify the error from the inner query is present in the result
        Assert.Contains(innerQueryError, result.Errors);

        // Verify the availability query WAS called using NSubstitute's Received
        await _availabilityQueryMock
            .Received(1)
            .ExecuteAsync(
                Arg.Is<AvailabilitySlotsQueryInput>(input => input.OwnerId == reviewer.Id), // Just check a basic param to confirm it was the expected call
                Arg.Any<CancellationToken>()
            );
    }
}
