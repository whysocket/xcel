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

public static class PageResultExtensions
{
    public static PageResult<TResult> Map<TSource, TResult>(
        this PageResult<TSource> source,
        Func<TSource, TResult> mapFunc)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(mapFunc);

        var mappedItems = source.Items.Select(mapFunc).ToList();

        return new PageResult<TResult>(
            mappedItems,
            source.Total,
            source.Pages,
            source.CurrentPage);
    }
}

public record PageRequest(
    int PageNumber,
    int PageSize);

public interface IGenericRepository<TEntity> where TEntity : BaseEntity
{
    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<TEntity?> GetByAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

    Task<PageResult<TEntity>> GetAllAsync(
        PageRequest pageRequest,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

    void Update(TEntity entity);

    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task RemoveByIdAsync(Guid id, CancellationToken cancellationToken = default);
    
    void Remove(TEntity entity);

    Task RemoveByAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);
}