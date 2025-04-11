﻿namespace Domain.Entities;

public abstract class BaseEntity
{
    public Guid Id { get; set; }
}

// -------------------- Person --------------------

public class Person : BaseEntity
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string EmailAddress { get; set; }

    public string FullName => $"{FirstName} {LastName}";
    public bool IsDeleted { get; set; }

    public TutorApplication? TutorApplication { get; set; }
}

// -------------------- TutorApplication (Aggregate Root) --------------------

public class TutorApplication : BaseEntity
{
    public enum OnboardingStep
    {
        CvUnderReview,
        AwaitingInterviewBooking,
        InterviewScheduled,
        DocumentsRequested,
        ServicesConfigured,
        ProfileValidated,
        Onboarded,
    }

    public OnboardingStep CurrentStep { get; set; } = OnboardingStep.CvUnderReview;

    public Guid PersonId { get; set; }
    public Person Person { get; set; } = null!;

    public List<TutorDocument> Documents { get; set; } = [];
    public Interview? Interview { get; set; }
    public TutorProfile? TutorProfile { get; set; }

    public List<FieldReview> FieldReviews { get; set; } = [];

    public bool IsRejected { get; set; }
}

// -------------------- TutorDocument --------------------

public class TutorDocument : BaseEntity
{
    public enum TutorDocumentType
    {
        Cv,
        Id,
        Dbs,
        Other
    }

    public enum TutorDocumentStatus
    {
        Pending,
        Approved,
        ResubmissionNeeded
    }

    public TutorDocumentType DocumentType { get; set; }
    public TutorDocumentStatus Status { get; set; }
    public required string DocumentPath { get; set; }
    public string? ModeratorReason { get; set; }

    public Guid TutorApplicationId { get; set; }
    public TutorApplication TutorApplication { get; set; } = null!;
}

// -------------------- Interview --------------------

public class Interview : BaseEntity
{
    public DateTime ScheduledAt { get; set; }
    public bool IsCompleted { get; set; }
    public bool IsSuccessful { get; set; }

    public Guid TutorApplicationId { get; set; }
    public TutorApplication TutorApplication { get; set; } = null!;
}

// -------------------- TutorProfile --------------------

public class TutorProfile : BaseEntity
{
    public required string SessionBio { get; set; }
    public required string FullBio { get; set; }
    public required string CardBio { get; set; } // max 120 characters

    public List<TutorService> TutorServices { get; set; } = [];

    public Guid TutorApplicationId { get; set; }
    public TutorApplication TutorApplication { get; set; } = null!;
}

// -------------------- TutorService --------------------

public class TutorService : BaseEntity
{
    public enum ServiceStatus
    {
        Pending,
        Approved,
        Rejected
    }

    public Guid TutorProfileId { get; set; }
    public TutorProfile TutorProfile { get; set; } = null!;

    public Guid QualificationId { get; set; }
    public Qualification Qualification { get; set; } = null!;

    public decimal PricePerHour { get; set; }
    public ServiceStatus Status { get; set; } = ServiceStatus.Pending;
}

// -------------------- FieldReview --------------------

public class FieldReview : BaseEntity
{
    public enum ReviewedField
    {
        SessionBio,
        FullBio,
        CardBio,
        Qualification,
        TutorServicePrice,
        Other
    }

    public enum FieldStatus
    {
        Approved,
        ResubmissionNeeded
    }

    public ReviewedField Field { get; set; }
    public FieldStatus Status { get; set; }
    public string? ModeratorReason { get; set; }

    public Guid TutorApplicationId { get; set; }
    public TutorApplication TutorApplication { get; set; } = null!;
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
}
