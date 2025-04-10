using Presentation.API.Endpoints.Admin.PersonRoles;
using Presentation.API.Endpoints.Admin.Roles;

namespace Presentation.API.Endpoints.Admin;

public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapRoleEndpoints()
            .MapPersonRoleEndpoints();
    }
}