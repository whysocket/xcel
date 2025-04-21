namespace Presentation.API.Endpoints;

public static class Endpoints
{
    private const string ApiVersion = "v1";
    private const string ApiPrefix = $"/{ApiVersion}";

    private static string BuildBasePath(params string[] segments)
    {
        return string.Join("/", new[] { ApiPrefix }.Concat(segments));
    }

    public static class TutorApplications
    {
        public static string BasePath => BuildBasePath("tutor-applications");
        
        public static readonly string My = $"{BasePath}/my";
    }

    public static class Moderator
    {
        public static class TutorApplications
        {
            public static string BasePath => BuildBasePath("moderators", "tutor-applications");

            public static readonly string Approve = $"{BasePath}/{{tutorApplicationId:guid}}/approve";
            public static readonly string Reject = $"{BasePath}/{{tutorApplicationId:guid}}/reject";
            public static readonly string ById = $"{BasePath}/{{tutorApplicationId:guid}}";
        }
    }

    public static class Admin
    {
        public static class Roles
        {
            public static string BasePath => BuildBasePath("admins", "roles");

            public static readonly string Update = $"{BasePath}/{{roleId:guid}}";
            public static readonly string Delete = $"{BasePath}/{{roleId:guid}}";
        }

        public static class PersonRoles
        {
            public static string BasePath => BuildBasePath("admins", "person-roles");

            public static readonly string Create = $"{BasePath}/{{personId:guid}}/roles/{{roleId:guid}}";
            public static readonly string GetAll = $"{BasePath}/{{personId:guid}}/roles";
            public static readonly string Delete = $"{BasePath}/{{personId:guid}}/roles/{{roleId:guid}}";
        }
    }

    public static class Accounts
    {
        public static string BasePath => BuildBasePath("accounts");

        public static string Login => $"{BasePath}/login";

        public static string LoginWithOtp => $"{BasePath}/login/otp";

        public static string Refresh => $"{BasePath}/refresh";
        
        public static string Delete => $"{BasePath}/delete/{{personId:guid}}";
    }

    public static class Reviewer
    {
        public static string BasePath => BuildBasePath("reviewers");

        public static class TutorApplications
        {
            public static string BasePath => BuildBasePath("reviewers", "tutor-applications");

            public static readonly string ProposeDates = $"{BasePath}/{{tutorApplicationId:guid}}/interview/propose";
            public static readonly string GetInterviewDetails = $"{BasePath}/{{tutorApplicationId:guid}}/interview";
        }
    }
}