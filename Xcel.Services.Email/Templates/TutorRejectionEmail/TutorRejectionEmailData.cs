namespace Xcel.Services.Email.Templates.TutorRejectionEmail;

public record TutorRejectionEmailData(
    string FirstName,
    string LastName,
    string? RejectionReason
);