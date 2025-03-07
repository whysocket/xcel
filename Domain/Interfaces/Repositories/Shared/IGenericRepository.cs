using Domain.Entities;
using System.Linq.Expressions;

namespace Domain.Interfaces.Repositories.Shared;

public record PageResult<TEntity>(
    List<TEntity> Items,
    int Total,
    int Pages,
    int CurrentPage)
{
    public bool HasNextPage => CurrentPage < Pages;
    public bool HasPreviousPage => CurrentPage > 1;
}

public record PageRequest(
    int PageNumber,
    int PageSize);

public interface IGenericRepository<TEntity> where TEntity : BaseEntity
{
    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PageResult<TEntity>> GetAllAsync(
        PageRequest pageRequest,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default);
}