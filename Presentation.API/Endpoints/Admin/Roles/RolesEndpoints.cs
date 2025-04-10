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
            .WithName("Roles.GetAll")
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
            .WithName("Roles.Create")
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
            .WithName("Roles.Update")
            .WithTags("Admin", "Roles");

        rolesGroup.MapDelete("/{roleId}", async (
                string roleName,
                IRoleService roleService,
                HttpContext httpContext) =>
            {
                var result = await roleService.DeleteRoleByNameAsync(roleName, httpContext.RequestAborted);
                return result.IsSuccess ? Results.NoContent() : result.MapProblemDetails();
            })
            .WithName("Roles.Delete")
            .WithTags("Admin", "Roles");
    }
}