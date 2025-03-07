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


public class Tutor : BaseEntity
{
    public required string FirstName { get; set; }

    public required string LastName { get; set; }
}