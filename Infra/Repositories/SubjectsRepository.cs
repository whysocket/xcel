using Domain.Entities;
using Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using Domain.Interfaces.Repositories.Shared;
using Infra.Repositories.Shared;
using Infra.Repositories.Extensions;

namespace Infra.Repositories;

public class SubjectsRepository(AppDbContext dbContext) : GenericRepository<Subject>(dbContext), ISubjectsRepository
{
    public async Task<bool> ExistsByName(string name, CancellationToken cancellationToken)
    {
        return await DbContext.Subjects.AnyAsync(s => s.Name == name, cancellationToken);
    }

    public async Task<PageResult<Subject>> GetAllWithQualificationsAsync(PageRequest pageRequest, CancellationToken cancellationToken = default)
    {
        return await DbContext
            .Subjects
            .Include(s => s.Qualifications)
            .WithPaginationAsync(pageRequest, cancellationToken);
    }
}
