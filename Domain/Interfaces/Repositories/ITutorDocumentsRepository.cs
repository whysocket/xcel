using Domain.Entities;
using Domain.Interfaces.Repositories.Shared;

namespace Domain.Interfaces.Repositories;

public interface ITutorDocumentsRepository : IGenericRepository<TutorDocument>
{
    Task<int> GetLatestDocumentVersionByType(
        Guid tutorApplicationId,
        TutorDocument.TutorDocumentType documentType, 
        CancellationToken cancellationToken = default);
}