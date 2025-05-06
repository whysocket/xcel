using Domain.Entities;
using Domain.Interfaces.Repositories;
using Domain.Interfaces.Repositories.Shared;
using Infra.Repositories.Extensions;
using Infra.Repositories.Shared;
using Microsoft.EntityFrameworkCore;

namespace Infra.Repositories.Auth;

internal class PersonsRepository(AppDbContext dbContext)
    : GenericRepository<Person>(dbContext),
        IPersonsRepository
{
    public override Task<Person?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        return DbContext
            .Set<Person>()
            .SingleOrDefaultAsync(p => p.Id == id && p.IsDeleted == false, cancellationToken);
    }

    public Task<Person?> GetByEmailAsync(
        string emailAddress,
        CancellationToken cancellationToken = default
    )
    {
        return DbContext
            .Set<Person>()
            .FirstOrDefaultAsync(
                p => p.EmailAddress == emailAddress && p.IsDeleted == false,
                cancellationToken
            );
    }

    public Task<List<Person>> GetAllByEmailAsync(
        IEnumerable<string> emailAddresses,
        CancellationToken cancellationToken = default
    )
    {
        return DbContext
            .Set<Person>()
            .Where(p => p.IsDeleted == false && emailAddresses.Contains(p.EmailAddress))
            .ToListAsync(cancellationToken);
    }

    public Task<Person?> GetDeletedByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return DbContext
            .Set<Person>()
            .SingleOrDefaultAsync(p => p.Id == id && p.IsDeleted == true, cancellationToken);
    }

    public Task<PageResult<Person>> GetAllDeletedAsync(
        PageRequest request,
        CancellationToken cancellationToken = default
    )
    {
        return DbContext
            .Set<Person>()
            .Where(p => p.IsDeleted == true)
            .WithPaginationAsync(request, cancellationToken);
    }
}
