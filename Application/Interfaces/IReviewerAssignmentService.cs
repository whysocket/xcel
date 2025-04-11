namespace Application.Interfaces;

public interface IReviewerAssignmentService
{
    Task<Person?> GetAvailableReviewerAsync(CancellationToken cancellationToken);
}