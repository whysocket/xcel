using Domain.Entities;
using Domain.Interfaces.Repositories;
using Infra.Repositories.Shared;

namespace Infra.Repositories.Auth;

internal class TutorProfilesRepository(AppDbContext dbContext)
    : GenericRepository<TutorProfile>(dbContext),
        ITutorProfilesRepository;
