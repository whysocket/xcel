using Domain.Entities;
using Domain.Interfaces.Repositories.Shared;

namespace Domain.Interfaces.Repositories;

public interface ITutorApplicationsRepository : IGenericRepository<TutorApplication>
{
    Task<List<TutorApplication>> GetAllWithDocumentsAndApplicantByOnboardingStep(
        TutorApplication.OnboardingStep onboardingStep,
        CancellationToken cancellationToken = default);

    Task<TutorApplication?> GetByIdWithDocumentsAndApplicantAsync(
        Guid tutorApplicationId,
        CancellationToken cancellationToken = default);

    Task<TutorApplication?> GetByIdWithInterviewAndPeopleAsync(Guid tutorApplicationId, CancellationToken cancellationToken = default);
    Task<TutorApplication?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);
}