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

    public async Task<List<TutorApplication>> GetAllPendingTutorsWithDocuments(CancellationToken cancellationToken = default)
    {
        return await DbContext
            .Set<TutorApplication>()
            .Include(t => t.Documents)
            .Include(t => t.Person)
            .Where(t => t.CurrentStep == TutorApplication.OnboardingStep.CvUnderReview)
            .ToListAsync(cancellationToken);
    }
}