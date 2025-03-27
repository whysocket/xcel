using Xcel.Services.Auth.Interfaces.Services;

namespace Presentation.API.Endpoints.Admin.PersonRoles;

internal static class PersonRoleEndpoints
{
    internal static void MapPersonRoleEndpoints(this RouteGroupBuilder rolesGroup)
    {
        rolesGroup.MapPost("/{personId}/roles/{roleId}", async (
                Guid personId,
                Guid roleId,
                IPersonRoleService personRoleService,
                HttpContext httpContext) =>
            {
                var result = await personRoleService.AddRoleToPersonAsync(personId, roleId, httpContext.RequestAborted);
                return result.IsSuccess ? Results.Created($"/admin/roles/{personId}/roles/{roleId}", null) : result.MapProblemDetails();
            })
            .WithName("AddRoleToPerson")
            .WithTags("Admin", "Roles");

        rolesGroup.MapGet("/{personId}/roles", async (
                Guid personId,
                IPersonRoleService personRoleService,
                HttpContext httpContext) =>
            {
                var result = await personRoleService.GetRolesForPersonAsync(personId, httpContext.RequestAborted);
                return result.IsSuccess ? Results.Ok(result.Value) : result.MapProblemDetails();
            })
            .WithName("GetRolesForPerson")
            .WithTags("Admin", "Roles");

        rolesGroup.MapDelete("/{personId}/roles/{roleId}", async (
                Guid personId,
                Guid roleId,
                IPersonRoleService personRoleService,
                HttpContext httpContext) =>
            {
                var result = await personRoleService.RemoveRoleFromPersonAsync(personId, roleId, httpContext.RequestAborted);
                return result.IsSuccess ? Results.NoContent() : result.MapProblemDetails();
            })
            .WithName("RemoveRoleFromPerson")
            .WithTags("Admin", "Roles");
    }
}