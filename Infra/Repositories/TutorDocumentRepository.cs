using Domain.Entities;
using Domain.Interfaces.Repositories;
using Infra.Repositories.Shared;
using Microsoft.EntityFrameworkCore;

namespace Infra.Repositories;

internal class TutorDocumentRepository(AppDbContext dbContext) : GenericRepository<TutorDocument>(dbContext), ITutorDocumentsRepository
{
    public async Task<int> GetLatestDocumentVersionByType(
        Guid tutorApplicationId,
        TutorDocument.TutorDocumentType documentType,
        CancellationToken cancellationToken = default)
    {
        var versions = await DbContext.Set<TutorDocument>()
            .Where(t => t.TutorApplicationId == tutorApplicationId && t.DocumentType == documentType)
            .Select(t => t.Version)
            .ToListAsync(cancellationToken);

        return versions.DefaultIfEmpty(0).Max();
    }
}