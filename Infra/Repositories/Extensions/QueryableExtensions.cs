using Domain.Interfaces.Repositories.Shared;
using Microsoft.EntityFrameworkCore;

namespace Infra.Repositories.Extensions;

internal static class QueryableExtensions
{
    public static async Task<PageResult<TEntity>> WithPaginationAsync<TEntity>(
        this IQueryable<TEntity> query,
        PageRequest pageRequest,
        CancellationToken cancellationToken = default) where TEntity : class
    {
        var total = await query.CountAsync(cancellationToken);
        var pages = (int)Math.Ceiling(total / (double)pageRequest.PageSize);

        var items = await query
            .Skip((pageRequest.PageNumber - 1) * pageRequest.PageSize)
            .Take(pageRequest.PageSize)
            .ToListAsync(cancellationToken);

        return new PageResult<TEntity>(items, total, pages, pageRequest.PageNumber);
    }
}