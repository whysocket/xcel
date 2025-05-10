using Domain.Entities;
using Domain.Interfaces.Repositories.Shared;

namespace Domain.Interfaces.Repositories;

public interface ITutorApplicationsRepository : IGenericRepository<TutorApplication>
{
    Task<List<TutorApplication>> GetAllWithDocumentsAndApplicantAndInterviewByOnboardingStep(
        TutorApplication.OnboardingStep onboardingStep,
        CancellationToken cancellationToken = default
    );

    Task<TutorApplication?> GetByIdWithDocumentsAndApplicantAsync(
        Guid tutorApplicationId,
        CancellationToken cancellationToken = default
    );

    Task<TutorApplication?> GetByIdWithInterviewAndPeopleAsync(
        Guid tutorApplicationId,
        CancellationToken cancellationToken = default
    );
    
    Task<TutorApplicationInterview?> GetBookingAtSlotAsync(
        Guid reviewerId,
        DateTime startUtc,
        DateTime endUtc,
        CancellationToken cancellationToken = default
    );
    
    Task<List<TutorApplicationInterview>> GetScheduledInterviewsForOwnerAsync(
        Guid ownerId,
        DateTime afterUtc,
        CancellationToken cancellationToken = default
    );
    
    Task<TutorApplication?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);
    Task<List<TutorApplication>> GetAllByReviewerIdAsync(
        Guid reviewerId,
        CancellationToken cancellationToken
    );
}
