using Presentation.API.Endpoints.Admin.Roles.Requests;
using Xcel.Services.Auth.Constants;
using Xcel.Services.Auth.Interfaces.Services;

namespace Presentation.API.Endpoints.Admin.Roles;

internal static class RolesEndpoints
{
    internal static IEndpointRouteBuilder MapRoleEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(Endpoints.Admin.Roles.BasePath, async (
                IRoleService roleService,
                HttpContext httpContext) =>
            {
                var result = await roleService.GetAllRolesAsync(cancellationToken: httpContext.RequestAborted);
                return result.IsSuccess ? Results.Ok(result.Value) : result.MapProblemDetails();
            })
            .WithName("Roles.GetAll")
            .WithSummary("Get all available roles.")
            .WithDescription("Retrieves a list of all roles defined within the system.")
            .WithTags(UserRoles.Admin)
            .RequireAuthorization(p => p.RequireRole(UserRoles.Admin));

        endpoints.MapPost(Endpoints.Admin.Roles.BasePath, async (
                CreateRoleRequest request,
                IRoleService roleService,
                HttpContext httpContext) =>
            {
                var result = await roleService.CreateRoleAsync(request.Name, httpContext.RequestAborted);
                return result.IsSuccess
                    ? Results.Created($"/admin/roles/{result.Value.Id}", result.Value)
                    : result.MapProblemDetails();
            })
            .WithName("Roles.Create")
            .WithSummary("Create a new role.")
            .WithDescription("Creates a new role with the specified name.")
            .WithTags(UserRoles.Admin)
            .RequireAuthorization(p => p.RequireRole(UserRoles.Admin));

        endpoints.MapPut(Endpoints.Admin.Roles.Update, async (
                Guid roleId,
                UpdateRoleRequest request,
                IRoleService roleService,
                HttpContext httpContext) =>
            {
                var result = await roleService.UpdateRoleAsync(roleId, request.Name, httpContext.RequestAborted);
                return result.IsSuccess ? Results.Ok() : result.MapProblemDetails();
            })
            .WithName("Roles.Update")
            .WithSummary("Update an existing role.")
            .WithDescription("Updates the name of an existing role, identified by its role ID.")
            .WithTags(UserRoles.Admin)
            .RequireAuthorization(p => p.RequireRole(UserRoles.Admin));

        endpoints.MapDelete(Endpoints.Admin.Roles.Delete, async (
                string roleName,
                IRoleService roleService,
                HttpContext httpContext) =>
            {
                var result = await roleService.DeleteRoleByNameAsync(roleName, httpContext.RequestAborted);
                return result.IsSuccess ? Results.NoContent() : result.MapProblemDetails();
            })
            .WithName("Roles.Delete")
            .WithSummary("Delete a role by name.")
            .WithDescription("Deletes a role based on its name.")
            .WithTags(UserRoles.Admin)
            .RequireAuthorization(p => p.RequireRole(UserRoles.Admin));
        
        return endpoints;
    }
}