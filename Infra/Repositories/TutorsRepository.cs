using Domain.Entities;
using Domain.Interfaces.Repositories;
using Infra.Repositories.Shared;
using Microsoft.EntityFrameworkCore;
using Xcel.Services.Auth.Interfaces.Repositories;

namespace Infra.Repositories;

internal class TutorsRepository(AppDbContext dbContext) : GenericRepository<Tutor>(dbContext), ITutorsRepository
{
    public async Task<Tutor?> GetTutorWithDocuments(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbContext.Tutors
            .Include(t => t.TutorDocuments)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<List<Tutor>> GetAllPendingTutorsWithDocuments(CancellationToken cancellationToken = default)
    {
        return await DbContext
            .Tutors
            .Include(t => t.TutorDocuments)
            .Include(t => t.Person)
            .Where(t => t.Status == Tutor.TutorStatus.Pending)
            .ToListAsync(cancellationToken);
    }
}