﻿namespace Domain.Entities;

public abstract class BaseEntity
{
    public Guid Id { get; set; }
}

// -------------------- Applicant --------------------
public class Person : BaseEntity
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string EmailAddress { get; set; }

    public string FullName => $"{FirstName} {LastName}";
    public bool IsDeleted { get; set; }

    public TutorApplication? TutorApplication { get; set; }

    public List<TutorApplicationInterview> ApplicationInterviews { get; set; } = [];
}

public class Role
{
    public Guid Id { get; set; }

    public required string Name { get; set; }
}

// -------------------- TutorApplicationResource (Aggregate Root) --------------------
public class TutorApplication : BaseEntity
{
    public enum OnboardingStep
    {
        CvAnalysis, // Step 1: Moderator reviews CV
        InterviewBooking, // Step 2: Interview is scheduled & confirmed
        DocumentsAnalysis, // Step 3: ID & DBS are submitted and reviewed
        Onboarded, // Step 4: Tutor configures bios/services
    }

    public OnboardingStep CurrentStep { get; set; } = OnboardingStep.CvAnalysis;

    public Guid ApplicantId { get; set; }
    public Person Applicant { get; set; } = null!;

    public List<TutorDocument> Documents { get; set; } = [];
    public TutorApplicationInterview? Interview { get; set; }

    public bool IsRejected { get; set; }
}

public enum AvailabilityRuleType
{
    /// <summary>
    /// A standard availability period, potentially recurring (weekly) or date-ranged, not a single day.
    /// Managed by SetAvailabilityRulesCommand.
    /// </summary>
    AvailabilityStandard,

    /// <summary>
    /// A specific availability period valid for only one day.
    /// Managed by AddOneOffAvailabilitySlotCommand.
    /// </summary>
    AvailabilityOneOff,

    /// <summary>
    /// An exclusion period that blocks out the entire day(s).
    /// Managed by AddExclusionPeriodCommand (FullDay type).
    /// </summary>
    ExclusionFullDay,

    /// <summary>
    /// An exclusion period that blocks out a specific time range on the day(s).
    /// Managed by AddExclusionPeriodCommand (SpecificTime type).
    /// </summary>
    ExclusionTimeBased,
}

public class AvailabilityRule : BaseEntity
{
    public Guid OwnerId { get; set; }
    public required Person Owner { get; set; }
    public AvailabilityOwnerType OwnerType { get; set; }

    public AvailabilityRuleType RuleType { get; set; }

    public DayOfWeek DayOfWeek { get; set; }

    public TimeSpan StartTimeUtc { get; set; }
    public TimeSpan EndTimeUtc { get; set; }

    public DateTime ActiveFromUtc { get; set; }
    public DateTime? ActiveUntilUtc { get; set; }
}

public enum AvailabilityOwnerType
{
    Reviewer,
    Tutor,
}

// -------------------- TutorDocument --------------------
public class TutorDocument : BaseEntity
{
    public enum TutorDocumentType
    {
        Cv,
        Id,
        Dbs,
        Other,
    }

    public enum TutorDocumentStatus
    {
        Pending,
        Approved,
        ResubmissionNeeded,
    }

    public TutorDocumentType DocumentType { get; set; }
    public TutorDocumentStatus Status { get; set; }
    public required string DocumentPath { get; set; }
    public string? ModeratorReason { get; set; }

    public int Version { get; set; } = 1;

    public Guid TutorApplicationId { get; set; }
    public TutorApplication TutorApplication { get; set; } = null!;
}

// -------------------- Interview --------------------
public class TutorApplicationInterview : BaseEntity
{
    public DateTime? ScheduledAtUtc { get; set; }

    public enum InterviewPlatform
    {
        GoogleMeets,
    }

    public InterviewPlatform Platform { get; set; }

    public enum InterviewStatus
    {
        AwaitingApplicantSlotSelection,
        Confirmed,
    }

    public InterviewStatus Status { get; set; }

    public string? Observations { get; set; }

    public Guid ReviewerId { get; set; }
    public required Person Reviewer { get; set; }

    public Guid TutorApplicationId { get; set; }
    public TutorApplication TutorApplication { get; set; } = null!;

    public Guid ConfirmedBy { get; set; }
}

// -------------------- TutorProfile --------------------

public class TutorProfile : BaseEntity
{
    public List<FieldVersion> FieldVersions { get; set; } = [];
    public List<TutorService> TutorServices { get; set; } = [];

    public Guid PersonId { get; set; }
    public Person Person { get; set; } = null!;

    public enum TutorProfileStatus
    {
        // It was already Onboarded but has 0 services, and at least one bio is pending (without previous versions)
        PendingConfiguration,
        Active,
        Inactive,
    }

    public TutorProfileStatus Status { get; set; }
}

// -------------------- TutorService --------------------
public class TutorService : BaseEntity
{
    public enum ServiceStatus
    {
        Pending,
        Approved,
        Rejected,
    }

    public Guid TutorProfileId { get; set; }
    public TutorProfile TutorProfile { get; set; } = null!;

    public Guid QualificationId { get; set; }
    public Qualification Qualification { get; set; } = null!;

    public decimal PricePerHour { get; set; }
    public ServiceStatus Status { get; set; } = ServiceStatus.Pending;
}

// -------------------- FieldVersion --------------------
public class FieldVersion : BaseEntity
{
    public enum Field
    {
        SessionBio,
        FullBio,
        CardBio,
        TutorServicePrice,
    }

    public Field FieldType { get; set; }

    public string? Value { get; set; }

    public DateTime CreatedAt { get; set; }

    public FieldStatus Status { get; set; } = FieldStatus.Pending;

    public string? ModeratorReason { get; set; }

    public int Version { get; set; } = 1;

    public Guid TutorProfileId { get; set; }
    public TutorProfile TutorProfile { get; set; } = null!;
}

public enum FieldStatus
{
    Pending,
    Approved,
    ResubmissionNeeded,
}

// -------------------- Subject & Qualification --------------------
public class Subject : BaseEntity
{
    public required string Name { get; set; }
    public List<Qualification> Qualifications { get; set; } = [];
}

public class Qualification : BaseEntity
{
    public required string Name { get; set; }

    public Guid SubjectId { get; set; }
    public Subject Subject { get; set; } = null!;

    public IEnumerable<Level> Levels { get; set; } = [];
}

public class Level : BaseEntity
{
    public required string Name { get; set; }

    public Guid QualificationId { get; set; }
    public Qualification Qualification { get; set; } = null!;
}
