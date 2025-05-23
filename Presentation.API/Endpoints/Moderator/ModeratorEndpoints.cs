using Presentation.API.Endpoints.Moderator.TutorApplication;

namespace Presentation.API.Endpoints.Moderator;

internal static class ModeratorEndpoints
{
    internal static IEndpointRouteBuilder MapModeratorEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        return endpoints.MapModeratorTutorApplicationEndpoints();
    }
}
