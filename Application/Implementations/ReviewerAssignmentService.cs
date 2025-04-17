using Application.Interfaces;
using Domain.Constants;

namespace Application.Implementations;

internal class ReviewerAssignmentService(IPersonsRepository personsRepository) : IReviewerAssignmentService
{
    internal static class Errors
    {
        internal static Error ReviewersUnavailability = new(ErrorType.Validation, "There is no reviewers");
    }

    public async Task<Result<Person>> GetAvailableReviewerAsync(CancellationToken cancellationToken)
    {
        var reviewers = await personsRepository.GetAllByEmailAsync(
            ReviewersConstants.ReviewersEmails,
            cancellationToken);
        
        var firstReviewer = reviewers.FirstOrDefault();
        if (firstReviewer == null)
        {
            return Result.Fail<Person>(Errors.ReviewersUnavailability);
        }

        return Result.Ok(firstReviewer);
    }
}