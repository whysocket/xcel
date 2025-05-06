using Domain.Constants;
using Presentation.API.Endpoints.Admin.Roles.Requests;
using Xcel.Services.Auth.Public;

namespace Presentation.API.Endpoints.Admin.Roles;

internal static class RolesEndpoints
{
    internal static IEndpointRouteBuilder MapRoleEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapGet(
                Endpoints.Admin.Roles.BasePath,
                async (IAuthServiceSdk authService, HttpContext httpContext) =>
                {
                    var result = await authService.GetAllRolesAsync(
                        cancellationToken: httpContext.RequestAborted
                    );
                    return result.IsSuccess ? Results.Ok(result.Value) : result.MapProblemDetails();
                }
            )
            .WithName("Role.GetAll")
            .WithSummary("Get all available roles.")
            .WithDescription("Retrieves a list of all roles defined within the system.")
            .WithTags(UserRoles.Admin)
            .RequireAuthorization(p => p.RequireRole(UserRoles.Admin));

        endpoints
            .MapPost(
                Endpoints.Admin.Roles.BasePath,
                async (
                    CreateRoleRequest request,
                    IAuthServiceSdk authService,
                    HttpContext httpContext
                ) =>
                {
                    var result = await authService.CreateRoleAsync(
                        request.Name,
                        httpContext.RequestAborted
                    );
                    return result.IsSuccess
                        ? Results.Created($"/admin/roles/{result.Value.Id}", result.Value)
                        : result.MapProblemDetails();
                }
            )
            .WithName("Role.CreateRequest")
            .WithSummary("CreateRequest a new role.")
            .WithDescription("Creates a new role with the specified name.")
            .WithTags(UserRoles.Admin)
            .RequireAuthorization(p => p.RequireRole(UserRoles.Admin));

        endpoints
            .MapPut(
                Endpoints.Admin.Roles.Update,
                async (
                    Guid roleId,
                    UpdateRoleRequest request,
                    IAuthServiceSdk authService,
                    HttpContext httpContext
                ) =>
                {
                    var result = await authService.UpdateRoleAsync(
                        roleId,
                        request.Name,
                        httpContext.RequestAborted
                    );
                    return result.IsSuccess ? Results.Ok() : result.MapProblemDetails();
                }
            )
            .WithName("Role.Update")
            .WithSummary("Update an existing role.")
            .WithDescription("Updates the name of an existing role, identified by its role ID.")
            .WithTags(UserRoles.Admin)
            .RequireAuthorization(p => p.RequireRole(UserRoles.Admin));

        endpoints
            .MapDelete(
                Endpoints.Admin.Roles.Delete,
                async (string roleName, IAuthServiceSdk authService, HttpContext httpContext) =>
                {
                    var result = await authService.DeleteRoleByNameAsync(
                        roleName,
                        httpContext.RequestAborted
                    );
                    return result.IsSuccess ? Results.NoContent() : result.MapProblemDetails();
                }
            )
            .WithName("Role.Delete")
            .WithSummary("Delete a role by name.")
            .WithDescription("Deletes a role based on its name.")
            .WithTags(UserRoles.Admin)
            .RequireAuthorization(p => p.RequireRole(UserRoles.Admin));

        return endpoints;
    }
}
