using Domain.Entities;
using Domain.Interfaces.Repositories.Shared;

namespace Domain.Interfaces.Repositories;

public interface ITutorApplicationsRepository : IGenericRepository<TutorApplication>
{
    Task<TutorApplication?> GetTutorWithDocuments(Guid id, CancellationToken cancellationToken = default);
    
    Task<List<TutorApplication>> GetAllPendingTutorsWithDocuments(CancellationToken cancellationToken = default);
}