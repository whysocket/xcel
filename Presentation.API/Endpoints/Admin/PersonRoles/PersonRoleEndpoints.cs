using Xcel.Services.Auth.Constants;
using Xcel.Services.Auth.Interfaces.Services;

namespace Presentation.API.Endpoints.Admin.PersonRoles;

internal static class PersonRoleEndpoints
{
    internal static IEndpointRouteBuilder MapPersonRoleEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost(Endpoints.Admin.PersonRoles.Create, async (
                Guid personId,
                Guid roleId,
                IPersonRoleService personRoleService,
                HttpContext httpContext) =>
            {
                var result = await personRoleService.AddRoleToPersonAsync(personId, roleId, httpContext.RequestAborted);
                return result.IsSuccess ? Results.Created(Endpoints.Admin.PersonRoles.GetAll, null) : result.MapProblemDetails();
            })
            .WithName("PersonRoles.Add")
            .WithSummary("Assign a role to a person.")
            .WithDescription("Assigns a specific role to a user, identified by their person ID.")
            .WithTags(UserRoles.Admin)
            .RequireAuthorization(p => p.RequireRole(UserRoles.Admin));

        endpoints.MapGet(Endpoints.Admin.PersonRoles.GetAll, async (
                Guid personId,
                IPersonRoleService personRoleService,
                HttpContext httpContext) =>
            {
                var result = await personRoleService.GetRolesForPersonAsync(personId, httpContext.RequestAborted);
                return result.IsSuccess ? Results.Ok(result.Value) : result.MapProblemDetails();
            })
            .WithName("PersonRoles.Get")
            .WithSummary("Get roles assigned to a person.")
            .WithDescription("Retrieves the list of roles assigned to a specific user, identified by their person ID.")
            .WithTags(UserRoles.Admin)
            .RequireAuthorization(p => p.RequireRole(UserRoles.Admin));

        endpoints.MapDelete(Endpoints.Admin.PersonRoles.Delete, async (
                Guid personId,
                Guid roleId,
                IPersonRoleService personRoleService,
                HttpContext httpContext) =>
            {
                var result = await personRoleService.RemoveRoleFromPersonAsync(personId, roleId, httpContext.RequestAborted);
                return result.IsSuccess ? Results.NoContent() : result.MapProblemDetails();
            })
            .WithName("PersonRoles.Remove")
            .WithSummary("Remove a role from a person.")
            .WithDescription("Removes a specific role from a user, identified by their person ID.")
            .WithTags(UserRoles.Admin)
            .RequireAuthorization(p => p.RequireRole(UserRoles.Admin));

        return endpoints;
    }
}