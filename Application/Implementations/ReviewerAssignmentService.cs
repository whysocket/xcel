using Application.Interfaces;
using Domain.Constants;
using Domain.Interfaces.Repositories.Shared;
using Xcel.Services.Auth.Public;

namespace Application.Implementations;

internal class ReviewerAssignmentService(
    IAuthServiceSdk authServiceSdk) : IReviewerAssignmentService
{
    internal static class Errors
    {
        internal static Error ReviewersUnavailability = new(ErrorType.Validation, "There is no reviewers");
    }

    public async Task<Result<Person>> GetAvailableReviewerAsync(CancellationToken cancellationToken = default)
    {
        var reviewerRoleResult = await authServiceSdk.GetRoleByNameAsync(UserRoles.Reviewer, cancellationToken);
        if (reviewerRoleResult.IsFailure)
        {
            return Result.Fail<Person>(reviewerRoleResult.Errors);
        }
        
        var reviewersResult = await authServiceSdk.GetAllPersonsByRoleIdAsync(
            reviewerRoleResult.Value.Id,
            new PageRequest(1, 100),
            cancellationToken);

        if (reviewersResult.IsFailure)
        {
            return Result.Fail<Person>(reviewersResult.Errors);
        }
        
        var firstReviewer = reviewersResult.Value.Items.FirstOrDefault();
        if (firstReviewer == null)
        {
            return Result.Fail<Person>(Errors.ReviewersUnavailability);
        }

        return Result.Ok(firstReviewer);
    }
}