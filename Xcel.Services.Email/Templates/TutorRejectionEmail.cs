namespace Xcel.Services.Email.Templates;

public record TutorRejectionEmail(
    string FullName,
    string? RejectionReason
);