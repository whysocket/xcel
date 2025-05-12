using Application.UseCases.Commands.TutorApplicationOnboarding.Applicant.Step3.BookInterview;
using Domain.Entities;
using Xcel.Services.Email.Templates;
using Xcel.TestUtils;

namespace Domain.IntegrationTests.UseCases.Queries.TutorApplicationOnboarding.Applicant.Step3;

// Integration tests for ApplicantBookInterviewSlotCommand
public class ApplicantBookInterviewSlotCommandTests : BaseTest
{
    private IApplicantBookInterviewSlotCommand _command = null!;

    // Removed Mock<IClientInfoService> - using FakeClientInfoService from BaseTest

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        // FakeClientInfoService is available from BaseTest
        _command = new ApplicantBookInterviewSlotCommand(
            TutorApplicationsRepository, // TutorApplicationsRepository available from BaseTest
            AvailabilityRulesRepository, // AvailabilityRulesRepository available from BaseTest
            InMemoryEmailService, // InMemoryEmailService available from BaseTest
            CreateLogger<ApplicantBookInterviewSlotCommand>(), // CreateLogger available from BaseTest
            FakeClientInfoService // Use the FakeClientInfoService directly from BaseTest
        );
    }

    [Fact]
    public async Task ExecuteAsync_ShouldBookSlot_WhenInputIsValid()
    {
        // Scenario: Successfully book an interview slot that is within an available rule and not double-booked.
        // Arrange
        var now = FakeTimeProvider.GetUtcNow().UtcDateTime;

        var applicant = new Person
        {
            FirstName = "Tina",
            LastName = "Wells",
            EmailAddress = "tina@xcel.com",
            Id = Guid.NewGuid(),
        };
        var reviewer = new Person
        {
            FirstName = "Lucas",
            LastName = "Day",
            EmailAddress = "lucas@xcel.com",
            Id = Guid.NewGuid(),
        };

        // Set the applicant Person in the FakeClientInfoService
        FakeClientInfoService.WithUser(applicant);

        // Add an availability rule for today
        var rule = new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            Owner = reviewer,
            OwnerId = reviewer.Id,
            OwnerType = AvailabilityOwnerType.Reviewer,
            RuleType = AvailabilityRuleType.AvailabilityStandard,
            DayOfWeek = now.DayOfWeek,
            StartTimeUtc = new(13, 0, 0),
            EndTimeUtc = new(16, 0, 0),
            ActiveFromUtc = now.Date,
            ActiveUntilUtc = now.Date,
        };

        var application = new TutorApplication
        {
            Id = Guid.NewGuid(),
            Applicant = applicant,
            ApplicantId = applicant.Id,
            Interview = new()
            {
                Id = Guid.NewGuid(),
                Reviewer = reviewer,
                ReviewerId = reviewer.Id,
                Status = TutorApplicationInterview.InterviewStatus.AwaitingApplicantSlotSelection,
            },
        };

        await PersonsRepository.AddRangeAsync([applicant, reviewer]);
        await AvailabilityRulesRepository.AddAsync(rule);
        await TutorApplicationsRepository.AddAsync(application);
        await TutorApplicationsRepository.SaveChangesAsync();

        var slot = now.Date.AddHours(13); // Slot starting at 13:00 today (within availability)
        var input = new ApplicantBookInterviewSlotInput(application.Id, slot, "Looking forward");

        // Act
        var result = await _command.ExecuteAsync(input);

        // Assert
        Assert.True(result.IsSuccess);

        var updatedApp = await TutorApplicationsRepository.GetByIdAsync(application.Id);
        Assert.NotNull(updatedApp);
        Assert.NotNull(updatedApp.Interview);

        Assert.Equal(
            TutorApplicationInterview.InterviewStatus.Confirmed,
            updatedApp!.Interview!.Status
        );
        Assert.Equal(slot, updatedApp.Interview.ScheduledAtUtc);
        // ConfirmedBy should be the Applicant's Person ID as per command logic
        Assert.Equal(application.ApplicantId, updatedApp.Interview.ConfirmedBy);

        var email = InMemoryEmailService.GetSentEmail<InterviewScheduledEmail>();
        Assert.NotNull(email);

        var payload = email.Payload.Data;
        Assert.Equal(applicant.FullName, payload.ApplicantFullName);
        Assert.Equal(reviewer.FullName, payload.ReviewerFullName);
        Assert.Equal(slot, payload.ScheduledAtUtc);

        var expectedEmail = new InterviewScheduledEmail(
            applicant.FullName,
            reviewer.FullName,
            slot
        );
        Assert.Equal(expectedEmail.Subject, email.Payload.Subject);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenInvalidSlot_NoAvailabilityRules()
    {
        // Scenario: Attempt to book a slot when no availability rules exist for the reviewer on that day.
        // Arrange
        var applicant = new Person
        {
            FirstName = "Bob",
            LastName = "Invalid",
            EmailAddress = "bob@xcel.com",
            Id = Guid.NewGuid(),
        };
        var reviewer = new Person
        {
            FirstName = "Zoe",
            LastName = "NoSlot",
            EmailAddress = "zoe@xcel.com",
            Id = Guid.NewGuid(),
        };

        // Set the applicant Person in the FakeClientInfoService
        FakeClientInfoService.WithUser(applicant);

        var application = new TutorApplication
        {
            Id = Guid.NewGuid(),
            Applicant = applicant,
            ApplicantId = applicant.Id,
            Interview = new()
            {
                Id = Guid.NewGuid(),
                Reviewer = reviewer,
                ReviewerId = reviewer.Id,
                Status = TutorApplicationInterview.InterviewStatus.AwaitingApplicantSlotSelection,
            },
        };

        await PersonsRepository.AddRangeAsync([applicant, reviewer]);
        // No availability rules added for the reviewer
        await TutorApplicationsRepository.AddAsync(application);
        await TutorApplicationsRepository.SaveChangesAsync();

        var slot = FakeTimeProvider.GetUtcNow().UtcDateTime.Date.AddHours(10); // A time on today's date
        var input = new ApplicantBookInterviewSlotInput(application.Id, slot, "No valid slot");

        // Act
        var result = await _command.ExecuteAsync(input);

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(ApplicantBookInterviewSlotCommandErrors.InvalidSlot.Message, error.Message);

        // Verify application status did not change
        var updatedApp = await TutorApplicationsRepository.GetByIdAsync(application.Id);
        Assert.NotNull(updatedApp);
        Assert.NotNull(updatedApp.Interview);
        Assert.Equal(
            TutorApplicationInterview.InterviewStatus.AwaitingApplicantSlotSelection,
            updatedApp!.Interview!.Status
        );
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenInvalidSlot_FallsOutsideAvailabilityRule()
    {
        // Scenario: Attempt to book a slot that is on a day with an availability rule, but outside the rule's time range.
        // Arrange
        var now = FakeTimeProvider.GetUtcNow().UtcDateTime;

        var applicant = new Person
        {
            FirstName = "Charlie",
            LastName = "Outside",
            EmailAddress = "charlie@xcel.com",
            Id = Guid.NewGuid(),
        };
        var reviewer = new Person
        {
            FirstName = "David",
            LastName = "Limited",
            EmailAddress = "david@xcel.com",
            Id = Guid.NewGuid(),
        };

        // Set the applicant Person in the FakeClientInfoService
        FakeClientInfoService.WithUser(applicant);

        // Add an availability rule for today from 14:00 to 16:00
        var rule = new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            Owner = reviewer,
            OwnerId = reviewer.Id,
            OwnerType = AvailabilityOwnerType.Reviewer,
            RuleType = AvailabilityRuleType.AvailabilityStandard, // Use RuleType
            DayOfWeek = now.DayOfWeek,
            StartTimeUtc = new(14, 0, 0), // 14:00 UTC
            EndTimeUtc = new(16, 0, 0), // 16:00 UTC
            ActiveFromUtc = now.Date,
            ActiveUntilUtc = now.Date,
        };

        var application = new TutorApplication
        {
            Id = Guid.NewGuid(),
            Applicant = applicant,
            ApplicantId = applicant.Id,
            Interview = new()
            {
                Id = Guid.NewGuid(),
                Reviewer = reviewer,
                ReviewerId = reviewer.Id,
                Status = TutorApplicationInterview.InterviewStatus.AwaitingApplicantSlotSelection,
            },
        };

        await PersonsRepository.AddRangeAsync([applicant, reviewer]);
        await AvailabilityRulesRepository.AddAsync(rule);
        await TutorApplicationsRepository.AddAsync(application);
        await TutorApplicationsRepository.SaveChangesAsync();

        var slot = now.Date.AddHours(10); // Slot starting at 10:00 today (outside 14:00-16:00 rule)
        var input = new ApplicantBookInterviewSlotInput(application.Id, slot, "Outside time");

        // Act
        var result = await _command.ExecuteAsync(input);

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(ApplicantBookInterviewSlotCommandErrors.InvalidSlot.Message, error.Message);

        // Verify application status did not change
        var updatedApp = await TutorApplicationsRepository.GetByIdAsync(application.Id);
        Assert.NotNull(updatedApp);
        Assert.NotNull(updatedApp.Interview);
        Assert.Equal(
            TutorApplicationInterview.InterviewStatus.AwaitingApplicantSlotSelection,
            updatedApp!.Interview!.Status
        );
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenInvalidSlot_OverlapsWithTimeBasedExclusion()
    {
        // Scenario: Attempt to book a slot that falls within an availability rule but overlaps with a time-based exclusion rule on the same day.
        // Arrange
        var now = FakeTimeProvider.GetUtcNow().UtcDateTime;

        var applicant = new Person
        {
            FirstName = "Eve",
            LastName = "Excluded",
            EmailAddress = "eve@xcel.com",
            Id = Guid.NewGuid(),
        };
        var reviewer = new Person
        {
            FirstName = "Frank",
            LastName = "Busy",
            EmailAddress = "frank@xcel.com",
            Id = Guid.NewGuid(),
        };

        // Set the applicant Person in the FakeClientInfoService
        FakeClientInfoService.WithUser(applicant);

        // Add a broad availability rule for today
        var availabilityRule = new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            Owner = reviewer,
            OwnerId = reviewer.Id,
            OwnerType = AvailabilityOwnerType.Reviewer,
            RuleType = AvailabilityRuleType.AvailabilityStandard, // Use RuleType
            DayOfWeek = now.DayOfWeek,
            StartTimeUtc = new(9, 0, 0), // 9:00 UTC
            EndTimeUtc = new(17, 0, 0), // 17:00 UTC
            ActiveFromUtc = now.Date,
            ActiveUntilUtc = now.Date,
        };
        // Add a time-based exclusion rule for today
        var exclusionRule = new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            Owner = reviewer,
            OwnerId = reviewer.Id,
            OwnerType = AvailabilityOwnerType.Reviewer,
            RuleType = AvailabilityRuleType.ExclusionTimeBased, // Use RuleType
            DayOfWeek = now.DayOfWeek,
            StartTimeUtc = new(12, 0, 0), // 12:00 UTC
            EndTimeUtc = new(13, 0, 0), // 13:00 UTC
            ActiveFromUtc = now.Date,
            ActiveUntilUtc = now.Date,
        };

        var application = new TutorApplication
        {
            Id = Guid.NewGuid(),
            Applicant = applicant,
            ApplicantId = applicant.Id,
            Interview = new()
            {
                Id = Guid.NewGuid(),
                Reviewer = reviewer,
                ReviewerId = reviewer.Id,
                Status = TutorApplicationInterview.InterviewStatus.AwaitingApplicantSlotSelection,
            },
        };

        await PersonsRepository.AddRangeAsync([applicant, reviewer]);
        await AvailabilityRulesRepository.AddRangeAsync([availabilityRule, exclusionRule]);
        await TutorApplicationsRepository.AddAsync(application);
        await TutorApplicationsRepository.SaveChangesAsync();

        var slot = now.Date.AddHours(12).AddMinutes(30); // Slot starting at 12:30 today (within availability, but within exclusion)
        var input = new ApplicantBookInterviewSlotInput(application.Id, slot, "Overlaps exclusion");

        // Act
        var result = await _command.ExecuteAsync(input);

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(ApplicantBookInterviewSlotCommandErrors.InvalidSlot.Message, error.Message);

        // Verify application status did not change
        var updatedApp = await TutorApplicationsRepository.GetByIdAsync(application.Id);
        Assert.NotNull(updatedApp);
        Assert.NotNull(updatedApp.Interview);
        Assert.Equal(
            TutorApplicationInterview.InterviewStatus.AwaitingApplicantSlotSelection,
            updatedApp!.Interview!.Status
        );
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenInvalidSlot_FallsOnFullDayExclusion()
    {
        // Scenario: Attempt to book a slot on a day that has a full-day exclusion rule.
        // Arrange
        var now = FakeTimeProvider.GetUtcNow().UtcDateTime;
        var excludedDate = now.Date.AddDays(1); // Tomorrow

        var applicant = new Person
        {
            FirstName = "George",
            LastName = "FullDay",
            EmailAddress = "george@xcel.com",
            Id = Guid.NewGuid(),
        };
        var reviewer = new Person
        {
            FirstName = "Helen",
            LastName = "Holiday",
            EmailAddress = "helen@xcel.com",
            Id = Guid.NewGuid(),
        };

        // Set the applicant Person in the FakeClientInfoService
        FakeClientInfoService.WithUser(applicant);

        // Add a full-day exclusion rule for tomorrow
        var exclusionRule = new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            Owner = reviewer,
            OwnerId = reviewer.Id,
            OwnerType = AvailabilityOwnerType.Reviewer,
            RuleType = AvailabilityRuleType.ExclusionFullDay, // Use RuleType
            DayOfWeek = excludedDate.DayOfWeek,
            StartTimeUtc = TimeSpan.Zero,
            EndTimeUtc = TimeSpan.FromDays(1), // Full day
            ActiveFromUtc = excludedDate,
            ActiveUntilUtc = excludedDate,
        };
        // Add an availability rule for tomorrow (should be ignored by the booking validation due to full-day exclusion)
        var availabilityRule = new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            Owner = reviewer,
            OwnerId = reviewer.Id,
            OwnerType = AvailabilityOwnerType.Reviewer,
            RuleType = AvailabilityRuleType.AvailabilityStandard, // Use RuleType
            DayOfWeek = excludedDate.DayOfWeek,
            StartTimeUtc = new(9, 0, 0),
            EndTimeUtc = new(17, 0, 0),
            ActiveFromUtc = excludedDate,
            ActiveUntilUtc = excludedDate,
        };

        var application = new TutorApplication
        {
            Id = Guid.NewGuid(),
            Applicant = applicant,
            ApplicantId = applicant.Id,
            Interview = new()
            {
                Id = Guid.NewGuid(),
                Reviewer = reviewer,
                ReviewerId = reviewer.Id,
                Status = TutorApplicationInterview.InterviewStatus.AwaitingApplicantSlotSelection,
            },
        };

        await PersonsRepository.AddRangeAsync([applicant, reviewer]);
        await AvailabilityRulesRepository.AddRangeAsync([exclusionRule, availabilityRule]);
        await TutorApplicationsRepository.AddAsync(application);
        await TutorApplicationsRepository.SaveChangesAsync();

        var slot = excludedDate.Date.AddHours(10); // Slot starting at 10:00 tomorrow (on excluded day)
        var input = new ApplicantBookInterviewSlotInput(application.Id, slot, "On excluded day");

        // Act
        var result = await _command.ExecuteAsync(input);

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(ApplicantBookInterviewSlotCommandErrors.InvalidSlot.Message, error.Message);

        // Verify application status did not change
        var updatedApp = await TutorApplicationsRepository.GetByIdAsync(application.Id);
        Assert.NotNull(updatedApp);
        Assert.NotNull(updatedApp.Interview);
        Assert.Equal(
            TutorApplicationInterview.InterviewStatus.AwaitingApplicantSlotSelection,
            updatedApp!.Interview!.Status
        );
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenInterviewNotInCorrectStatus()
    {
        // Scenario: Attempt to book a slot when the interview status is not AwaitingApplicantSlotSelection.
        // Arrange
        var applicant = new Person
        {
            FirstName = "Alex",
            LastName = "WrongState",
            EmailAddress = "alex@xcel.com",
            Id = Guid.NewGuid(),
        };
        var reviewer = new Person
        {
            FirstName = "Dana",
            LastName = "StillWaiting",
            EmailAddress = "dana@xcel.com",
            Id = Guid.NewGuid(),
        };

        // Set the applicant Person in the FakeClientInfoService
        FakeClientInfoService.WithUser(applicant);

        var application = new TutorApplication
        {
            Id = Guid.NewGuid(),
            Applicant = applicant,
            ApplicantId = applicant.Id,
            Interview = new()
            {
                Id = Guid.NewGuid(),
                Reviewer = reviewer,
                ReviewerId = reviewer.Id,
                Status = TutorApplicationInterview.InterviewStatus.Confirmed, // Wrong status
            },
        };

        await PersonsRepository.AddRangeAsync([applicant, reviewer]);
        await TutorApplicationsRepository.AddAsync(application);
        await TutorApplicationsRepository.SaveChangesAsync();

        var input = new ApplicantBookInterviewSlotInput(
            application.Id,
            FakeTimeProvider.GetUtcNow().UtcDateTime.Date.AddHours(14),
            null
        );

        var result = await _command.ExecuteAsync(input);

        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(
            ApplicantBookInterviewSlotCommandErrors.InterviewNotSelectable.Message,
            error.Message
        );

        // Verify application status did not change
        var updatedApp = await TutorApplicationsRepository.GetByIdAsync(application.Id);
        Assert.NotNull(updatedApp);
        Assert.NotNull(updatedApp.Interview);
        Assert.Equal(
            TutorApplicationInterview.InterviewStatus.Confirmed,
            updatedApp!.Interview!.Status
        );
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenInterviewMissing()
    {
        // Scenario: Attempt to book a slot when the TutorApplication does not have an associated Interview.
        // Arrange
        var applicant = new Person
        {
            FirstName = "Missing",
            LastName = "Interview",
            EmailAddress = "missing@xcel.com",
            Id = Guid.NewGuid(),
        };

        // Set the applicant Person in the FakeClientInfoService
        FakeClientInfoService.WithUser(applicant);

        var application = new TutorApplication
        {
            Id = Guid.NewGuid(),
            Applicant = applicant,
            ApplicantId = applicant.Id,
            Interview = null!, // Interview is null
        };

        await PersonsRepository.AddAsync(applicant);
        await TutorApplicationsRepository.AddAsync(application);
        await TutorApplicationsRepository.SaveChangesAsync();

        var input = new ApplicantBookInterviewSlotInput(
            application.Id,
            FakeTimeProvider.GetUtcNow().UtcDateTime,
            "none"
        );
        var result = await _command.ExecuteAsync(input);

        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(
            ApplicantBookInterviewSlotCommandErrors.ApplicationOrInterviewNotFound.Message,
            error.Message
        );

        // Verify application status did not change (it has no status to change)
        var updatedApp = await TutorApplicationsRepository.GetByIdAsync(application.Id);
        Assert.NotNull(updatedApp);
        Assert.Null(updatedApp.Interview);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldFail_WhenSlotAlreadyBooked()
    {
        // Scenario: Attempt to book a slot that is valid according to availability rules but is already booked by another interview.
        // Arrange
        var now = FakeTimeProvider.GetUtcNow().UtcDateTime;

        var applicant1 = new Person
        {
            FirstName = "Igor",
            LastName = "First",
            EmailAddress = "igor1@xcel.com",
            Id = Guid.NewGuid(),
        };
        var applicant2 = new Person
        {
            FirstName = "Jane",
            LastName = "Second",
            EmailAddress = "jane2@xcel.com",
            Id = Guid.NewGuid(),
        };
        var reviewer = new Person
        {
            FirstName = "Ken",
            LastName = "Reviewer",
            EmailAddress = "ken@xcel.com",
            Id = Guid.NewGuid(),
        };

        // Set the applicant Person in the FakeClientInfoService for the second applicant
        FakeClientInfoService.WithUser(applicant2);

        // Add an availability rule covering the slot
        var rule = new AvailabilityRule
        {
            Id = Guid.NewGuid(),
            Owner = reviewer,
            OwnerId = reviewer.Id,
            OwnerType = AvailabilityOwnerType.Reviewer,
            RuleType = AvailabilityRuleType.AvailabilityStandard, // Use RuleType
            DayOfWeek = now.DayOfWeek,
            StartTimeUtc = new(10, 0, 0), // 10:00 UTC
            EndTimeUtc = new(11, 0, 0), // 11:00 UTC
            ActiveFromUtc = now.Date,
            ActiveUntilUtc = now.Date,
        };

        // Create the first application and book the slot
        var bookedSlotTime = now.Date.AddHours(10); // Slot at 10:00 today
        var application1 = new TutorApplication
        {
            Id = Guid.NewGuid(),
            Applicant = applicant1,
            ApplicantId = applicant1.Id,
            Interview = new()
            {
                Id = Guid.NewGuid(),
                Reviewer = reviewer,
                ReviewerId = reviewer.Id,
                Status = TutorApplicationInterview.InterviewStatus.Confirmed, // Already Confirmed
                ScheduledAtUtc = bookedSlotTime, // This slot is booked
            },
        };

        // Create the second application (attempting to book the same slot)
        var application2 = new TutorApplication
        {
            Id = Guid.NewGuid(),
            Applicant = applicant2,
            ApplicantId = applicant2.Id,
            Interview = new()
            {
                Id = Guid.NewGuid(),
                Reviewer = reviewer,
                ReviewerId = reviewer.Id,
                Status = TutorApplicationInterview.InterviewStatus.AwaitingApplicantSlotSelection, // Attempting to book
            },
        };

        await PersonsRepository.AddRangeAsync([applicant1, applicant2, reviewer]);
        await AvailabilityRulesRepository.AddAsync(rule);
        await TutorApplicationsRepository.AddRangeAsync([application1, application2]);
        await TutorApplicationsRepository.SaveChangesAsync();

        // Input for the second applicant trying to book the same slot
        var input = new ApplicantBookInterviewSlotInput(
            application2.Id,
            bookedSlotTime,
            "Trying to double book"
        );

        // Act
        var result = await _command.ExecuteAsync(input);

        // Assert
        Assert.True(result.IsFailure);
        var error = Assert.Single(result.Errors);
        Assert.Equal(
            ApplicantBookInterviewSlotCommandErrors.SlotAlreadyBooked.Message,
            error.Message
        ); // Check message

        // Verify application2 status did not change
        var updatedApp2 = await TutorApplicationsRepository.GetByIdAsync(application2.Id);
        Assert.NotNull(updatedApp2);
        Assert.NotNull(updatedApp2.Interview);
        Assert.Equal(
            TutorApplicationInterview.InterviewStatus.AwaitingApplicantSlotSelection,
            updatedApp2!.Interview!.Status
        );
        Assert.Null(updatedApp2.Interview.ScheduledAtUtc); // Slot should not be booked

        // Verify application1 status remains Confirmed
        var updatedApp1 = await TutorApplicationsRepository.GetByIdAsync(application1.Id);
        Assert.NotNull(updatedApp1);
        Assert.NotNull(updatedApp1.Interview);
        Assert.Equal(
            TutorApplicationInterview.InterviewStatus.Confirmed,
            updatedApp1!.Interview!.Status
        );
        Assert.Equal(bookedSlotTime, updatedApp1.Interview.ScheduledAtUtc); // Original booking remains
    }
}
