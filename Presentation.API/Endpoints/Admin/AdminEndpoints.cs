using Presentation.API.Endpoints.Admin.PersonRoles;
using Presentation.API.Endpoints.Admin.Roles;
using Presentation.API.Endpoints.TutorApplication;

namespace Presentation.API.Endpoints.Admin;

public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var adminGroup = endpoints.MapGroup("/admin");
        // .RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" });

        // Tutor Applicants Endpoints
        adminGroup
            .MapGroup("/tutor-applicants")
            .MapAdminTutorApplicantEndpoints();

        // Roles Endpoints
        adminGroup
            .MapGroup("/roles")
            .MapRoleEndpoints();

        // Person Role Endpoints
        adminGroup
            .MapPersonRoleEndpoints();

        return endpoints;
    }
}