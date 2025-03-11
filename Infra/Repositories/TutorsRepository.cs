using Domain.Entities;
using Domain.Interfaces.Repositories;
using Infra.Repositories.Shared;
using Microsoft.EntityFrameworkCore;

namespace Infra.Repositories;

public class TutorsRepository(AppDbContext dbContext) : GenericRepository<Tutor>(dbContext), ITutorsRepository
{
    public async Task<Tutor?> GetTutorWithDocuments(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbContext.Tutors
            .Include(t => t.TutorDocuments)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }
}
