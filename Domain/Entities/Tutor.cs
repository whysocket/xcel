namespace Domain.Entities;

public abstract class BaseEntity
{
    public Guid Id { get; set; }
}

public class Subject : BaseEntity
{
    public required string Name { get; set; }

    public List<Qualification> Qualifications { get; set; } = [];
}

public class Qualification : BaseEntity
{
    public required string Name { get; set; }

    public Subject Subject { get; set; } = null!;
    public Guid SubjectId { get; set; }
}

public class Person : BaseEntity
{
    public required string FirstName { get; set; }

    public required string LastName { get; set; }

    public required string EmailAddress { get; set; }
}

public class Tutor : BaseEntity
{
    public enum OnboardingStep
    {
        ApplicationStarted,
        DocumentsUploaded,
        ServicesConfigured,
        ProfileValidated,
        Onboarded,
        ApplicationDenied
    }

    public OnboardingStep CurrentStep { get; set; }

    public List<TutorService> TutorServices { get; set; } = [];
    public List<TutorDocument> TutorDocuments { get; set; } = [];
    public Guid PersonId { get; set; }
    public Person Person { get; set; } = null!;
}

public class TutorService : BaseEntity
{
    public Guid TutorId { get; set; }
    public Tutor Tutor { get; set; } = null!;
    public Guid QualificationId { get; set; }
    public Qualification Qualification { get; set; } = null!;
    public decimal PricePerHour { get; set; }

    public enum TutorServiceStatus
    {
        Pending,
        Approved,
        Rejected
    }

    public TutorServiceStatus Status { get; set; }
}

public class TutorDocument : BaseEntity
{
    public enum TutorDocumentType
    {
        DBS,
        ID,
        CV,
        Other
    }
    public TutorDocumentType DocumentType { get; set; }
    public enum TutorDocumentStatus
    {
        Pending,
        Approved,
        Rejected
    }

    public TutorDocumentStatus Status { get; set; }

    public required string DocumentPath { get; set; }
    public Tutor Tutor { get; set; } = null!;
    public Guid TutorId { get; set; }
}