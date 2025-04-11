using Application.Interfaces;
using Domain.Constants;

namespace Application.Implementations;

internal class ReviewerAssignmentService(IPersonsRepository personsRepository) : IReviewerAssignmentService
{
    public async Task<Person?> GetAvailableReviewerAsync(CancellationToken cancellationToken)
    {
        var reviewers = await personsRepository.GetAllByEmailAsync(
            ReviewersConstants.ReviewersEmails,
            cancellationToken);
        
        return reviewers.FirstOrDefault();
    }
}