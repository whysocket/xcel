﻿using System.Linq.Expressions;
using Domain.Entities;
using Domain.Interfaces.Repositories.Shared;
using Infra.Repositories.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Infra.Repositories.Shared;

internal class GenericRepository<TEntity>(AppDbContext dbContext) : IGenericRepository<TEntity>
    where TEntity : BaseEntity
{
    protected readonly AppDbContext DbContext = dbContext;

    public virtual async Task<TEntity?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        return await DbContext.Set<TEntity>().FindAsync([id], cancellationToken);
    }

    public async Task<TEntity?> GetByAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default
    )
    {
        return await DbContext.Set<TEntity>().FirstOrDefaultAsync(predicate, cancellationToken);
    }

    public Task<PageResult<TEntity>> GetAllAsync(
        PageRequest pageRequest,
        Expression<Func<TEntity, object>>? orderBy = null,
        CancellationToken cancellationToken = default
    )
    {
        IQueryable<TEntity> query = DbContext.Set<TEntity>();

        if (orderBy != null)
        {
            query = query.OrderBy(orderBy);
        }

        return query.WithPaginationAsync(pageRequest, cancellationToken);
    }

    public Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default)
    {
        return DbContext.Set<TEntity>().CountAsync(cancellationToken);
    }

    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await DbContext.Set<TEntity>().AddAsync(entity, cancellationToken);
    }

    public async Task AddRangeAsync(
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default
    )
    {
        await DbContext.Set<TEntity>().AddRangeAsync(entities, cancellationToken);
    }

    public void Update(TEntity entity)
    {
        DbContext.Set<TEntity>().Update(entity);
    }

    public async Task<bool> ExistsAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default
    )
    {
        return await DbContext.Set<TEntity>().AnyAsync(predicate, cancellationToken);
    }

    public async Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await DbContext.SaveChangesAsync(cancellationToken) > 0;
    }

    public async Task RemoveByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await DbContext.Set<TEntity>().FindAsync([id], cancellationToken);
        if (entity != null)
        {
            DbContext.Set<TEntity>().Remove(entity);
            await SaveChangesAsync(cancellationToken);
        }
    }

    public void Remove(TEntity entity)
    {
        DbContext.Set<TEntity>().Remove(entity);
    }

    public async Task RemoveByAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default
    )
    {
        var entity = await DbContext
            .Set<TEntity>()
            .FirstOrDefaultAsync(predicate, cancellationToken);
        if (entity != null)
        {
            DbContext.Set<TEntity>().Remove(entity);
        }
    }
}
