using Domain.Interfaces.Repositories;
using Infra.Repositories.Shared;

namespace Infra.Repositories;

using Domain.Entities;
using Microsoft.EntityFrameworkCore;

internal class TutorDocumentRepository(AppDbContext dbContext)
    : GenericRepository<TutorDocument>(dbContext),
        ITutorDocumentsRepository
{
    public async Task<int> GetLatestDocumentVersionByType(
        Guid tutorApplicationId,
        TutorDocument.TutorDocumentType documentType,
        CancellationToken cancellationToken = default
    )
    {
        var versions = await DbContext
            .Set<TutorDocument>()
            .Where(t =>
                t.TutorApplicationId == tutorApplicationId && t.DocumentType == documentType
            )
            .Select(t => t.Version)
            .ToListAsync(cancellationToken);

        return versions.DefaultIfEmpty(0).Max();
    }

    public async Task<
        Dictionary<TutorDocument.TutorDocumentType, int>
    > GetLatestApprovedVersionsAsync(
        Guid tutorApplicationId,
        IEnumerable<TutorDocument.TutorDocumentType> documentTypes,
        CancellationToken cancellationToken = default
    )
    {
        var query = await DbContext
            .Set<TutorDocument>()
            .Where(d =>
                d.TutorApplicationId == tutorApplicationId
                && documentTypes.Contains(d.DocumentType)
                && d.Status == TutorDocument.TutorDocumentStatus.Approved
            )
            .GroupBy(d => d.DocumentType)
            .Select(g => new { DocumentType = g.Key, LatestVersion = g.Max(x => x.Version) })
            .ToListAsync(cancellationToken);

        return query.ToDictionary(x => x.DocumentType, x => x.LatestVersion);
    }
}
