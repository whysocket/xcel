using Domain.Entities;
using Domain.Interfaces.Repositories.Shared;

namespace Domain.Interfaces.Repositories;

public interface ITutorApplicationsRepository : IGenericRepository<TutorApplication>
{
    Task<TutorApplication?> GetTutorWithDocuments(Guid id, CancellationToken cancellationToken = default);
    
    Task<List<TutorApplication>> GetAllWithDocumentsAndApplicantByOnboardingStep(
        TutorApplication.OnboardingStep onboardingStep,
        CancellationToken cancellationToken = default);

    Task<TutorApplication?> GetByIdWithDocumentsAndApplicantAsync(
        Guid tutorApplicationId,
        CancellationToken cancellationToken = default);

    Task<TutorApplication?> GetByIdWithInterviewAndPeopleAsync(Guid tutorApplicationId, CancellationToken cancellationToken = default);
}