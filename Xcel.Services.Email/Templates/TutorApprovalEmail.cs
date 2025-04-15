namespace Xcel.Services.Email.Templates;

public record TutorApprovalEmail(
    string FullName
) 
{
    public const string Subject = "Your CV has been approved. Please select the proposed dates";
}