using Application.Interfaces;
using Domain.Constants;

namespace Application.Implementations;

internal class ReviewerAssignmentService(IPersonsRepository personsRepository) : IReviewerAssignmentService
{
    public async Task<Result<Person>> GetAvailableReviewerAsync(CancellationToken cancellationToken)
    {
        var reviewers = await personsRepository.GetAllByEmailAsync(
            ReviewersConstants.ReviewersEmails,
            cancellationToken);
        
        var firstReviewer = reviewers.FirstOrDefault();
        if (firstReviewer == null)
        {
            return Result.Fail<Person>(new Error(ErrorType.Validation, $"There is no reviewers"));
        }

        return Result.Ok(firstReviewer);
    }
}