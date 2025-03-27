using Xcel.Services.Auth.Interfaces.Services;

namespace Presentation.API.Endpoints.Admin.Roles;

internal static class RolesEndpoints
{
    internal static void MapRoleEndpoints(this RouteGroupBuilder rolesGroup)
    {
        rolesGroup.MapGet("/", async (
                IRoleService roleService,
                HttpContext httpContext) =>
            {
                var result = await roleService.GetAllRolesAsync(cancellationToken: httpContext.RequestAborted);
                return result.IsSuccess ? Results.Ok(result.Value) : result.MapProblemDetails();
            })
            .WithName("GetRoles")
            .WithTags("Admin", "Roles");

        rolesGroup.MapPost("/", async (
                CreateRoleRequest request,
                IRoleService roleService,
                HttpContext httpContext) =>
            {
                var result = await roleService.CreateRoleAsync(request.Name, httpContext.RequestAborted);
                return result.IsSuccess
                    ? Results.Created($"/admin/roles/{result.Value.Id}", result.Value)
                    : result.MapProblemDetails();
            })
            .WithName("CreateRole")
            .WithTags("Admin", "Roles");

        rolesGroup.MapPut("/{roleId}", async (
                Guid roleId,
                UpdateRoleRequest request,
                IRoleService roleService,
                HttpContext httpContext) =>
            {
                var result = await roleService.UpdateRoleAsync(roleId, request.Name, httpContext.RequestAborted);
                return result.IsSuccess ? Results.Ok() : result.MapProblemDetails();
            })
            .WithName("UpdateRole")
            .WithTags("Admin", "Roles");

        rolesGroup.MapDelete("/{roleId}", async (
                string roleName,
                IRoleService roleService,
                HttpContext httpContext) =>
            {
                var result = await roleService.DeleteRoleByNameAsync(roleName, httpContext.RequestAborted);
                return result.IsSuccess ? Results.NoContent() : result.MapProblemDetails();
            })
            .WithName("DeleteRole")
            .WithTags("Admin", "Roles");
    }
}