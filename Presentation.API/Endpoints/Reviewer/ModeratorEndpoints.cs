using Presentation.API.Endpoints.Reviewer.Availability;
using Presentation.API.Endpoints.Reviewer.TutorApplication;

namespace Presentation.API.Endpoints.Reviewer;

internal static class ReviewerEndpoints
{
    internal static IEndpointRouteBuilder MapReviewerEndpoints(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapReviewerTutorApplicationEndpoints().MapReviewerAvailabilityEndpoints();
    }
}
