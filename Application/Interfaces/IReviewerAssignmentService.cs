namespace Application.Interfaces;

public interface IReviewerAssignmentService
{
    Task<Result<Person>> GetAvailableReviewerAsync(CancellationToken cancellationToken);
}