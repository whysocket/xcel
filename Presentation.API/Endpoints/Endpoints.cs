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

        public static readonly string ReviewerAvailabilityByDate =
            $"{BasePath}/interview/reviewer-availability";
    }

    public static class Moderator
    {
        public static class TutorApplications
        {
            public static string BasePath => BuildBasePath("moderators", "tutor-applications");

            public static readonly string Approve =
                $"{BasePath}/{{tutorApplicationId:guid}}/approve";
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

            public static readonly string Create =
                $"{BasePath}/{{personId:guid}}/roles/{{roleId:guid}}";
            public static readonly string GetAll = $"{BasePath}/{{personId:guid}}/roles";
            public static readonly string Delete =
                $"{BasePath}/{{personId:guid}}/roles/{{roleId:guid}}";
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

            public static readonly string Reschedule =
                $"{BasePath}/{{tutorApplicationId:guid}}/interview/reschedule";
            public static readonly string GetAssignedInterviews = $"{BasePath}/interviews";
        }

        public static class Availability
        {
            public static string BasePath => BuildBasePath("reviewers", "availability");

            public static readonly string SetRecurring = $"{BasePath}/rules";
            public static readonly string AddOneOff = $"{BasePath}/one-off";
            public static readonly string AddExclusions = $"{BasePath}/exclusions";
            public static readonly string GetRules = $"{BasePath}/rules";
        }
    }
}
