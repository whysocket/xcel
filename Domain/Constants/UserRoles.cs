namespace Domain.Constants;

public static class UserRoles
{
    public const string Admin = "admin";
    public const string Moderator = "moderator";
    public const string Reviewer = "reviewer";

    public static readonly string[] All = [Admin, Moderator, Reviewer];
}