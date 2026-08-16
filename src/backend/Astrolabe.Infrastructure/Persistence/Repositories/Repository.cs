using System.Linq.Expressions;
using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Abstractions.Persistence;
using Astrolabe.Domain.Primitives;
using Microsoft.EntityFrameworkCore;

namespace Astrolabe.Infrastructure.Persistence.Repositories;

/// <summary>
/// Base implementation of the generic persistence contract.
///
/// It exists to remove the identical boilerplate every repository would otherwise repeat, not to
/// expose Entity Framework. <see cref="Query"/> and <see cref="ReadOnlyQuery"/> are
/// <c>protected</c>: derived repositories compose domain-specific queries with them, but no
/// IQueryable ever leaves this assembly, per GUIDELINES.md section 14.
/// </summary>
public abstract class Repository<TEntity>(AstrolabeDbContext context) : IRepository<TEntity>
    where TEntity : Entity
{
    protected AstrolabeDbContext Context { get; } = context;

    protected DbSet<TEntity> Set => Context.Set<TEntity>();

    /// <summary>Tracked query. The default for reads, so a returned entity can be mutated and saved.</summary>
    protected IQueryable<TEntity> Query => Set;

    /// <summary>
    /// Untracked query. For projections and counts, where tracking would cost memory for nothing.
    /// Never return its entities to a caller who may mutate them.
    /// </summary>
    protected IQueryable<TEntity> ReadOnlyQuery => Set.AsNoTracking();

    // ---------- Reads: single ----------

    public virtual async Task<TEntity?> GetByIdAsync(
        Guid id, CancellationToken cancellationToken = default) =>
        await Set.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public virtual async Task<TEntity?> GetByFilterAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return await Query.FirstOrDefaultAsync(predicate, cancellationToken);
    }

    // ---------- Reads: many ----------

    public virtual async Task<IReadOnlyList<TEntity>> GetAllAsync(
        CancellationToken cancellationToken = default) =>
        await Query.ToListAsync(cancellationToken);

    public virtual async Task<IReadOnlyList<TEntity>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ids);

        // Short-circuits an empty request rather than emitting `WHERE id IN ()`, which returns
        // nothing anyway and plans poorly.
        if (ids.Count == 0)
        {
            return [];
        }

        return await Query
            .Where(e => ids.Contains(e.Id))
            .ToListAsync(cancellationToken);
    }

    public virtual async Task<IReadOnlyList<TEntity>> ListByFilterAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return await Query.Where(predicate).ToListAsync(cancellationToken);
    }

    public virtual async Task<PagedResult<TEntity>> GetPagedAsync(
        int page,
        int pageSize,
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        var (safePage, safeSize) = PagedResult<TEntity>.Normalise(page, pageSize);

        var query = predicate is null ? Query : Query.Where(predicate);

        // Counted before paging, so the total reflects every match rather than the page.
        var total = await query.CountAsync(cancellationToken);

        if (total == 0)
        {
            return PagedResult<TEntity>.Empty(safePage, safeSize);
        }

        // Ordered by identifier because PostgreSQL gives no stable order otherwise, which would let
        // the same row appear on two pages. Concrete repositories override this with a meaningful
        // sort where one exists.
        var items = await query
            .OrderBy(e => e.Id)
            .Skip((safePage - 1) * safeSize)
            .Take(safeSize)
            .ToListAsync(cancellationToken);

        return PagedResult<TEntity>.Create(items, safePage, safeSize, total);
    }

    // ---------- Aggregates ----------

    public virtual async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default) =>
        await ReadOnlyQuery.AnyAsync(e => e.Id == id, cancellationToken);

    public virtual async Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return await ReadOnlyQuery.AnyAsync(predicate, cancellationToken);
    }

    public virtual async Task<int> CountAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default) =>
        predicate is null
            ? await ReadOnlyQuery.CountAsync(cancellationToken)
            : await ReadOnlyQuery.CountAsync(predicate, cancellationToken);

    // ---------- Writes ----------

    public virtual async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        await Set.AddAsync(entity, cancellationToken);
    }

    public virtual async Task AddRangeAsync(
        IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entities);
        await Set.AddRangeAsync(entities, cancellationToken);
    }

    public virtual void Update(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        Set.Update(entity);
    }

    public virtual void Remove(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        Set.Remove(entity);
    }

    public virtual void RemoveRange(IEnumerable<TEntity> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);
        Set.RemoveRange(entities);
    }
}
