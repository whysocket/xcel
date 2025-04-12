namespace Xcel.Services.Email.Templates.TutorApprovalEmail;

public record TutorApprovalEmailData(
    string FirstName,
    string LastName
)
{
    public const string Subject = "Your CV has been approved. Please select the proposed dates";
}