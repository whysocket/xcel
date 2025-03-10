using Domain.Entities;
using Domain.Interfaces.Repositories;
using Infra.Repositories.Shared;

namespace Infra.Repositories;

public class TutorsRepository(AppDbContext dbContext) : GenericRepository<Tutor>(dbContext), ITutorsRepository;