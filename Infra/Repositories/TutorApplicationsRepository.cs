using Domain.Entities;
using Domain.Interfaces.Repositories;
using Infra.Repositories.Shared;
using Microsoft.EntityFrameworkCore;

namespace Infra.Repositories;

internal class TutorApplicationsRepository(AppDbContext dbContext) : GenericRepository<TutorApplication>(dbContext), ITutorApplicationsRepository
{
    public async Task<TutorApplication?> GetTutorWithDocuments(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<TutorApplication>()
            .Include(t => t.Documents)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<List<TutorApplication>> GetAllWithDocumentsAndApplicantByOnboardingStep(
        TutorApplication.OnboardingStep onboardingStep,
        CancellationToken cancellationToken = default)
    {
        return await DbContext
            .Set<TutorApplication>()
            .Include(t => t.Documents)
            .Include(t => t.Applicant)
            .Where(t => t.CurrentStep == onboardingStep)
            .ToListAsync(cancellationToken);
    }
    
    public async Task<TutorApplication?> GetByIdWithDocumentsAndApplicantAsync(
        Guid tutorApplicationId,
        CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<TutorApplication>()
            .Include(a => a.Applicant)
            .Include(a => a.Documents)
            .FirstOrDefaultAsync(a => a.Id == tutorApplicationId, cancellationToken);
    }

    public async Task<TutorApplication?> GetByIdWithInterviewAndPeopleAsync(
        Guid tutorApplicationId,
        CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<TutorApplication>()
            .Include(t => t.Applicant)
            .Include(t => t.Interview)
            .ThenInclude(i => i!.Reviewer)
            .FirstOrDefaultAsync(t => t.Id == tutorApplicationId, cancellationToken);
    }
}